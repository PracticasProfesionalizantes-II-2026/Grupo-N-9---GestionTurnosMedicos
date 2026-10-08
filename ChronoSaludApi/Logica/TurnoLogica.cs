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
    private readonly IReloj _reloj;

    public TurnoLogica(
        ITurnoRepository repo,
        IPacienteRepository pacienteRepo,
        IDoctorRepository doctorRepo,
        INotificacionRepository notifRepo,
        ILogger<TurnoLogica> logger,
        IHorarioLaboralLogica horarioLogica,
        IRegistroMovimientos movimientos,
        IReloj reloj)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _doctorRepo = doctorRepo;
        _notifRepo = notifRepo;
        _logger = logger;
        _horarioLogica = horarioLogica;
        _movimientos = movimientos;
        _reloj = reloj;
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
                Mensaje   = mensaje,
                Fecha     = _reloj.Ahora()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo crear la notificación de turno para el paciente {IdPaciente}.", idPaciente);
        }
    }

    public async Task<(int total, IEnumerable<TurnoListaDto> turnos, Dictionary<string, int> conteos, string? error)> ObtenerTodos(
        FiltroTurnos filtro, string orden, bool descendente, int pagina, int limite,
        int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor)
    {
        var sinConteos = new Dictionary<string, int>();

        // El paciente/doctor autenticado nunca elige de quién ve la agenda:
        // se ignora lo que venga en la query y se fuerza siempre lo propio.
        if (callerEsPaciente)
        {
            var paciente = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (paciente == null)
                return (0, Enumerable.Empty<TurnoListaDto>(), sinConteos, "Tu usuario no tiene un perfil de paciente asociado.");
            filtro.PacienteId = paciente.Id;
        }
        else if (callerEsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (doctor == null)
                return (0, Enumerable.Empty<TurnoListaDto>(), sinConteos, "Tu usuario no tiene un perfil de doctor asociado.");
            filtro.DoctorId = doctor.Id;
        }

        // El total y la página salen de la base; los conteos por estado, también.
        var (total, turnos) = await _repo.Buscar(filtro, orden, descendente, pagina, limite);
        var conteos = await _repo.ContarPorEstado(filtro);

        var resultado = turnos
            .Select(t => new TurnoListaDto(
                t.Id,
                t.FechaInicio,
                t.HoraInicio.ToString(@"hh\:mm"),
                t.Estado,
                $"{t.Doctor?.Usuario?.Nombre} {t.Doctor?.Usuario?.Apellido}",
                t.Doctor?.Especialidad ?? string.Empty,
                $"{t.Paciente?.Usuario?.Nombre} {t.Paciente?.Usuario?.Apellido}"
            ))
            .ToList();

        return (total, resultado, conteos, null);
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

        // El paciente no reserva un horario que ya pasó. El personal sí puede
        // cargar uno de hoy (por ejemplo, alguien que se atendió sin turno).
        if (solicitante.EsPaciente && YaEmpezo(dto.FechaInicio, horaInicio))
            return (null, "Ese horario ya pasó. Elegí uno más adelante.");

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
            Estado       = EstadosTurno.Pendiente,
            Observaciones = dto.Observaciones
        };

        try
        {
            await _repo.Agregar(turno);
        }
        catch (DatoRepetidoException)
        {
            // Dos reservas del mismo horario al mismo tiempo: las dos pasaron
            // el chequeo de arriba, pero la base (índice único) frenó la segunda.
            return (null, "Conflicto de horario: ese horario se acaba de ocupar. Elegí otro.");
        }

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

        // 1. Reprogramar. Lo que no viene en el pedido queda como estaba.
        var nuevaFecha = dto.FechaInicio ?? turno.FechaInicio;
        var nuevaHoraInicio = turno.HoraInicio;
        var nuevaHoraFin = turno.HoraFin;

        if (!string.IsNullOrEmpty(dto.HoraInicio) && !TimeSpan.TryParse(dto.HoraInicio, out nuevaHoraInicio))
            return (false, "Formato de hora inicio inválido.", false);
        if (!string.IsNullOrEmpty(dto.HoraFin) && !TimeSpan.TryParse(dto.HoraFin, out nuevaHoraFin))
            return (false, "Formato de hora fin inválido.", false);

        var cambiaHorario = nuevaFecha != turno.FechaInicio
                            || nuevaHoraInicio != turno.HoraInicio
                            || nuevaHoraFin != turno.HoraFin;

        if (cambiaHorario)
        {
            if (!EstadosTurno.EstaEnPie(turno.Estado))
                return (false, $"Conflicto de estado: el turno está {turno.Estado} y solo se reprograma uno pendiente o confirmado.", false);

            // El horario nuevo pasa por las mismas reglas que un turno nuevo.
            var (horarioOk, errorHorario) = await _horarioLogica.ValidarHorarioLaboral(
                turno.IdDoctor, nuevaFecha, nuevaHoraInicio, nuevaHoraFin);
            if (!horarioOk)
                return (false, errorHorario, false);

            var conflicto = await _repo.HayConflictoHorario(turno.IdDoctor, nuevaFecha, nuevaHoraInicio, nuevaHoraFin, id);
            if (conflicto)
                return (false, "Conflicto de horario al reprogramar.", false);

            turno.FechaInicio = nuevaFecha;
            turno.HoraInicio  = nuevaHoraInicio;
            turno.HoraFin     = nuevaHoraFin;
        }

        // 2. Estado: solo los cambios que permite EstadosTurno.
        if (!string.IsNullOrWhiteSpace(dto.Estado))
        {
            var nuevoEstado = dto.Estado.Trim().ToLowerInvariant();
            var yaEmpezo = YaEmpezo(turno.FechaInicio, turno.HoraInicio);

            var (puede, motivo) = EstadosTurno.PuedeCambiar(turno.Estado, nuevoEstado, yaEmpezo);
            if (!puede)
                return (false, motivo, false);

            turno.Estado = nuevoEstado;
        }

        if (!string.IsNullOrEmpty(dto.Observaciones)) turno.Observaciones = dto.Observaciones;

        try
        {
            await _repo.Actualizar(turno);
        }
        catch (DatoRepetidoException)
        {
            return (false, "Conflicto de horario: ese horario se acaba de ocupar.", false);
        }

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

        if (!EstadosTurno.EstaEnPie(turno.Estado))
            return (false, $"Conflicto de estado: el turno está {turno.Estado} y no se puede cancelar.");

        // Un turno que ya empezó lo resuelve el personal (completado o
        // ausente): el paciente ya no lo cancela.
        if (solicitante.EsPaciente && YaEmpezo(turno.FechaInicio, turno.HoraInicio))
            return (false, "Ese turno ya empezó y no se puede cancelar desde tu cuenta. Si hace falta, hablá con la administración.");

        var estadoPrevio = turno.Estado;

        turno.Estado = EstadosTurno.Cancelado;
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
    /// ser las dos. Cualquier otro estado nuevo se registra como "estado
    /// cambiado" (hoy no pasa: EstadosTurno ya no deja guardar otros).
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
                "ausente"    => AccionesMovimiento.TurnoAusente,
                "cancelado"  => AccionesMovimiento.TurnoCancelado,
                _            => AccionesMovimiento.TurnoEstadoCambiado
            };

        if (accionDelEstado != null)
        {
            await _movimientos.Registrar(solicitante, accionDelEstado, "turno", turno.Id, turno.IdDoctor,
                $"Turno #{turno.Id} del {cuandoNuevo}: {EstadoParaResumen(estadoAnterior)} a {EstadoParaResumen(turno.Estado)}.");
        }
    }

    /// <summary>
    /// El estado de un turno para el resumen del historial. Antes PUT /turnos
    /// guardaba cualquier texto como estado, y en la base puede haber quedado
    /// alguno: al historial solo llegan los estados conocidos. Cualquier otro
    /// se anota como "otro estado", sin copiar nada de lo que se escribió.
    /// </summary>
    private static string EstadoParaResumen(string? estado) =>
        EstadosTurno.Todos.Contains(estado) ? estado! : "otro estado";

    /// <summary>
    /// Si ya llegó la hora de inicio del turno, en hora de Argentina. Un turno
    /// de hoy a las 10:00 empezó a las 10:00 en punto.
    /// </summary>
    private bool YaEmpezo(DateTime fecha, TimeSpan horaInicio) =>
        _reloj.Ahora() >= fecha.Date + horaInicio;

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
