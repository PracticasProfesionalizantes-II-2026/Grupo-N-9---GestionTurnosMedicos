using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class TurnoLogica : ITurnoLogica
{
    private readonly ITurnoRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly INotificacionRepository _notifRepo;
    private readonly ILogger<TurnoLogica> _logger;
    private readonly IHorarioLaboralLogica _horarioLogica;
    private readonly IRegistroMovimientos _movimientos;

    public TurnoLogica(
        ITurnoRepository repo,
        IPacienteRepository pacienteRepo,
        IDoctorRepository doctorRepo,
        INotificacionRepository notifRepo,
        ILogger<TurnoLogica> logger,
        IHorarioLaboralLogica horarioLogica,
        IRegistroMovimientos movimientos)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _doctorRepo = doctorRepo;
        _notifRepo = notifRepo;
        _logger = logger;
        _horarioLogica = horarioLogica;
        _movimientos = movimientos;
    }

    // Un fallo al insertar la notificación nunca puede hacer fracasar ni
    // parecer que fracasó la operación principal que ya se guardó.
    private async Task NotificarPaciente(int idPaciente, string mensaje)
    {
        try
        {
            var paciente = await _pacienteRepo.ObtenerPorId(idPaciente);
            if (paciente?.Usuario == null) return;

            await _notifRepo.Agregar(new Notificacion
            {
                IdUsuario = paciente.Usuario.Id,
                Tipo      = "turno",
                Mensaje   = mensaje
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo crear la notificación de turno para el paciente {IdPaciente}.", idPaciente);
        }
    }

    public async Task<(int total, IEnumerable<TurnoListaDto> turnos, string? error)> ObtenerTodos(
        int? pacienteId, int? doctorId, string? estado,
        DateTime? desde, DateTime? hasta, int pagina, int limite,
        int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor)
    {
        // El paciente/doctor autenticado nunca elige de quién ve la agenda:
        // se ignora lo que venga en la query y se fuerza siempre lo propio.
        if (callerEsPaciente)
        {
            var paciente = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (paciente == null)
                return (0, Enumerable.Empty<TurnoListaDto>(), "Tu usuario no tiene un perfil de paciente asociado.");
            pacienteId = paciente.Id;
        }
        else if (callerEsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (doctor == null)
                return (0, Enumerable.Empty<TurnoListaDto>(), "Tu usuario no tiene un perfil de doctor asociado.");
            doctorId = doctor.Id;
        }

        var todos = await _repo.ObtenerTodos(pacienteId, doctorId, estado, desde, hasta);
        var total = todos.Count();
        var resultado = todos
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .Select(t => new TurnoListaDto(
                t.Id,
                t.FechaInicio,
                t.HoraInicio.ToString(@"hh\:mm"),
                t.Estado,
                $"{t.Doctor?.Usuario?.Nombre} {t.Doctor?.Usuario?.Apellido}",
                t.Doctor?.Especialidad ?? string.Empty,
                $"{t.Paciente?.Usuario?.Nombre} {t.Paciente?.Usuario?.Apellido}"
            ));
        return (total, resultado, null);
    }

    public async Task<(TurnoDto? turno, string? error)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor, bool callerEsStaff)
    {
        var t = await _repo.ObtenerPorId(id);
        if (t == null) return (null, "Turno no encontrado.");

        if (!callerEsStaff)
        {
            if (callerEsPaciente)
            {
                var paciente = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
                if (paciente == null)
                    return (null, "Tu usuario no tiene un perfil de paciente asociado.");
                if (paciente.Id != t.IdPaciente)
                    return (null, "Turno no encontrado.");
            }
            else if (callerEsDoctor)
            {
                var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
                if (doctor == null)
                    return (null, "Tu usuario no tiene un perfil de doctor asociado.");
                if (doctor.Id != t.IdDoctor)
                    return (null, "Turno no encontrado.");
            }
            else
            {
                return (null, "Turno no encontrado.");
            }
        }

        return (new TurnoDto(
            t.Id,
            t.FechaInicio,
            t.HoraInicio.ToString(@"hh\:mm"),
            t.HoraFin.ToString(@"hh\:mm"),
            t.Estado,
            t.IdPaciente,
            t.IdDoctor,
            t.Observaciones
        ), null);
    }

    public async Task<(int? id, string? error)> Crear(TurnoCreateDto dto, Solicitante solicitante)
    {
        if (solicitante.EsPaciente)
        {
            // El paciente reserva solo a su nombre: su perfil sale del token y
            // el IdPaciente que venga en el cuerpo se ignora.
            var propio = await _pacienteRepo.ObtenerPorIdUsuario(solicitante.IdUsuario);
            if (propio == null)
                return (null, "Tu usuario no tiene un perfil de paciente asociado.");

            dto = dto with { IdPaciente = propio.Id };
        }
        else if (await _pacienteRepo.ObtenerPorId(dto.IdPaciente) == null)
        {
            // Sin este chequeo el alta fallaba recién en la base, con un 500.
            return (null, "El paciente indicado no existe.");
        }

        if (!TimeSpan.TryParse(dto.HoraInicio, out var horaInicio))
            return (null, "Formato de hora inicio inválido. Use HH:MM.");
        if (!TimeSpan.TryParse(dto.HoraFin, out var horaFin))
            return (null, "Formato de hora fin inválido. Use HH:MM.");

        var (horarioOk, errorHorario) = await _horarioLogica.ValidarHorarioLaboral(dto.IdDoctor, dto.FechaInicio, horaInicio, horaFin);
        if (!horarioOk)
            return (null, errorHorario);

        var conflicto = await _repo.HayConflictoHorario(dto.IdDoctor, dto.FechaInicio, horaInicio, horaFin);
        if (conflicto)
            return (null, "Conflicto de horario: el doctor ya tiene un turno en ese rango.");

        var turno = new Turno
        {
            IdPaciente   = dto.IdPaciente,
            IdDoctor     = dto.IdDoctor,
            FechaInicio  = dto.FechaInicio,
            HoraInicio   = horaInicio,
            HoraFin      = horaFin,
            Estado       = "pendiente",
            Observaciones = dto.Observaciones
        };

        await _repo.Agregar(turno);

        await _movimientos.Registrar(solicitante, AccionesMovimiento.TurnoCreado, "turno", turno.Id, turno.IdDoctor,
            $"Turno #{turno.Id} del {Cuando(turno)}: creado.");

        await NotificarPaciente(turno.IdPaciente,
            $"Se creó tu turno para el {turno.FechaInicio:dd/MM/yyyy} a las {dto.HoraInicio}.");

        return (turno.Id, null);
    }

    public async Task<(bool ok, string? error, bool sinPerfilDoctor)> Actualizar(
        int id, TurnoUpdateDto dto, Solicitante solicitante)
    {
        var turno = await _repo.ObtenerPorId(id);
        if (turno == null) return (false, "Turno no encontrado.", false);

        // El doctor solo mueve su propia agenda. Un turno de otro doctor se
        // responde como inexistente, igual que en el resto de la fase.
        if (solicitante.EsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(solicitante.IdUsuario);
            if (doctor == null)
                return (false, "Tu usuario no tiene un perfil de doctor asociado.", true);
            if (doctor.Id != turno.IdDoctor)
                return (false, "Turno no encontrado.", false);
        }

        var estadoAnterior = turno.Estado;
        var cuandoAnterior = Cuando(turno);

        if (dto.FechaInicio.HasValue) turno.FechaInicio = dto.FechaInicio.Value;

        if (!string.IsNullOrEmpty(dto.HoraInicio))
        {
            if (!TimeSpan.TryParse(dto.HoraInicio, out var hi))
                return (false, "Formato de hora inicio inválido.", false);
            turno.HoraInicio = hi;
        }
        if (!string.IsNullOrEmpty(dto.HoraFin))
        {
            if (!TimeSpan.TryParse(dto.HoraFin, out var hf))
                return (false, "Formato de hora fin inválido.", false);
            turno.HoraFin = hf;
        }

        // Verificar conflicto si se modificó la fecha u hora
        if (dto.FechaInicio.HasValue || dto.HoraInicio != null || dto.HoraFin != null)
        {
            var conflicto = await _repo.HayConflictoHorario(turno.IdDoctor, turno.FechaInicio, turno.HoraInicio, turno.HoraFin, id);
            if (conflicto)
                return (false, "Conflicto de horario al reprogramar.", false);
        }

        if (!string.IsNullOrEmpty(dto.Estado))       turno.Estado        = dto.Estado;
        if (!string.IsNullOrEmpty(dto.Observaciones)) turno.Observaciones = dto.Observaciones;

        await _repo.Actualizar(turno);

        await RegistrarCambios(turno, estadoAnterior, cuandoAnterior, solicitante);

        if (!string.IsNullOrEmpty(dto.Estado) && dto.Estado != estadoAnterior)
        {
            await NotificarPaciente(turno.IdPaciente,
                $"El estado de tu turno del {turno.FechaInicio:dd/MM/yyyy} cambió a \"{turno.Estado}\".");
        }

        return (true, null, false);
    }

    public async Task<(bool ok, string? error)> Cancelar(int id, Solicitante solicitante)
    {
        var turno = await _repo.ObtenerPorId(id);
        if (turno == null) return (false, "Turno no encontrado.");

        // Primero el dueño y después el estado: un turno ajeno se responde
        // como inexistente y nunca llega a revelar en qué estado está.
        if (!await PuedeCancelar(turno, solicitante))
            return (false, "Turno no encontrado.");

        if (turno.Estado != "pendiente" && turno.Estado != "confirmado")
            return (false, $"Conflicto de estado: el turno está {turno.Estado} y no se puede cancelar.");

        var estadoPrevio = turno.Estado;

        turno.Estado = "cancelado";
        await _repo.Eliminar(turno);

        await _movimientos.Registrar(solicitante, AccionesMovimiento.TurnoCancelado, "turno", turno.Id, turno.IdDoctor,
            $"Turno #{turno.Id} del {Cuando(turno)}: {EstadoParaResumen(estadoPrevio)} a cancelado.");

        await NotificarPaciente(turno.IdPaciente,
            $"Tu turno del {turno.FechaInicio:dd/MM/yyyy} fue cancelado.");

        return (true, null);
    }

    /// <summary>
    /// Lo que deja en el historial un PUT de turno: "reprogramado" si cambió el
    /// día o la hora, y la acción del estado nuevo si cambió el estado. Pueden
    /// ser las dos. Un estado nuevo que no es confirmado, completado ni
    /// cancelado se registra como "estado cambiado".
    /// </summary>
    private async Task RegistrarCambios(Turno turno, string estadoAnterior, string cuandoAnterior, Solicitante solicitante)
    {
        var cuandoNuevo = Cuando(turno);

        if (cuandoNuevo != cuandoAnterior)
        {
            await _movimientos.Registrar(solicitante, AccionesMovimiento.TurnoReprogramado, "turno", turno.Id, turno.IdDoctor,
                $"Turno #{turno.Id}: del {cuandoAnterior} al {cuandoNuevo}.");
        }

        var accionDelEstado = turno.Estado == estadoAnterior
            ? null
            : turno.Estado switch
            {
                "confirmado" => AccionesMovimiento.TurnoConfirmado,
                "completado" => AccionesMovimiento.TurnoCompletado,
                "cancelado"  => AccionesMovimiento.TurnoCancelado,
                _            => AccionesMovimiento.TurnoEstadoCambiado
            };

        if (accionDelEstado != null)
        {
            await _movimientos.Registrar(solicitante, accionDelEstado, "turno", turno.Id, turno.IdDoctor,
                $"Turno #{turno.Id} del {cuandoNuevo}: {EstadoParaResumen(estadoAnterior)} a {EstadoParaResumen(turno.Estado)}.");
        }
    }

    // Los únicos estados que se nombran en el historial.
    private static readonly string[] EstadosConocidos = ["pendiente", "confirmado", "completado", "cancelado"];

    /// <summary>
    /// El estado de un turno para el resumen del historial. PUT /turnos guarda
    /// cualquier texto como estado, y ese texto puede traer cualquier cosa: al
    /// historial solo llegan los cuatro estados conocidos. Cualquier otro se
    /// anota como "otro estado", sin copiar nada de lo que se escribió.
    /// </summary>
    private static string EstadoParaResumen(string? estado) =>
        EstadosConocidos.Contains(estado) ? estado! : "otro estado";

    /// <summary>Día y horas del turno para el resumen del historial: "14/10/2026 de 13:00 a 13:30".</summary>
    private static string Cuando(Turno turno) =>
        $"{turno.FechaInicio:dd/MM/yyyy} de {turno.HoraInicio:hh\\:mm} a {turno.HoraFin:hh\\:mm}";

    /// <summary>
    /// El administrador cancela cualquier turno; el paciente, los suyos; el
    /// doctor, los de su agenda. Un rol que no es ninguno de esos, o un
    /// paciente o doctor sin perfil, no cancela nada.
    /// </summary>
    private async Task<bool> PuedeCancelar(Turno turno, Solicitante solicitante)
    {
        if (solicitante.EsAdministrador) return true;

        if (solicitante.EsPaciente)
        {
            var paciente = await _pacienteRepo.ObtenerPorIdUsuario(solicitante.IdUsuario);
            return paciente != null && paciente.Id == turno.IdPaciente;
        }

        if (solicitante.EsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(solicitante.IdUsuario);
            return doctor != null && doctor.Id == turno.IdDoctor;
        }

        return false;
    }
}
