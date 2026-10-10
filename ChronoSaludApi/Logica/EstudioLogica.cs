using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class EstudioLogica : IEstudioLogica
{
    private readonly IEstudioRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly ITurnoRepository _turnoRepo;
    private readonly INotificacionRepository _notifRepo;
    private readonly IReloj _reloj;
    private readonly ILogger<EstudioLogica> _logger;

    public EstudioLogica(
        IEstudioRepository repo,
        IPacienteRepository pacienteRepo,
        IDoctorRepository doctorRepo,
        ITurnoRepository turnoRepo,
        INotificacionRepository notifRepo,
        IReloj reloj,
        ILogger<EstudioLogica> logger)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _doctorRepo = doctorRepo;
        _turnoRepo = turnoRepo;
        _notifRepo = notifRepo;
        _reloj = reloj;
        _logger = logger;
    }

    public async Task<(IEnumerable<EstudioDto> estudios, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, string? tipo, string? estado, DateTime? desde, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (Enumerable.Empty<EstudioDto>(), "Tu usuario no tiene un perfil de paciente asociado.", false);
            if (propio.Id != pacienteId)
                return (Enumerable.Empty<EstudioDto>(), null, true);
        }

        var estudios = await _repo.ObtenerDePaciente(pacienteId, tipo, estado, desde);
        return (estudios.Select(MapearDto), null, false);
    }

    public async Task<(EstudioDto? estudio, string? error, bool prohibido)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsStaff)
    {
        // Mismo criterio que ObtenerDePaciente: el staff ve cualquier estudio y
        // el paciente solo los propios. El perfil se resuelve antes de buscar el
        // estudio para que "no tenés perfil" no dependa de que el id exista.
        Paciente? propio = null;
        if (!callerEsStaff)
        {
            propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (null, "Tu usuario no tiene un perfil de paciente asociado.", false);
        }

        var e = await _repo.ObtenerConDoctor(id);
        if (e == null) return (null, "Estudio no encontrado.", false);

        if (propio != null && propio.Id != e.IdPaciente)
            return (null, null, true);

        return (MapearDto(e), null, false);
    }

    /// <summary>
    /// Lo pide un doctor (el del token). Si viene un turno, tiene que ser de
    /// este paciente con este doctor, como en recetas e historia clínica.
    /// </summary>
    public async Task<(int? id, string? error)> Crear(EstudioCreateDto dto, int idUsuarioCaller)
    {
        var tiposValidos = new[] { "sangre", "imagen", "biopsia", "otro" };
        if (!tiposValidos.Contains(dto.Tipo))
            return (null, "Tipo inválido. Valores: sangre, imagen, biopsia, otro.");

        // Sin esto, un paciente inexistente se iba contra la foreign key y salía como 500.
        var paciente = await _pacienteRepo.ObtenerPorId(dto.IdPaciente);
        if (paciente == null)
            return (null, $"Paciente con id {dto.IdPaciente} no encontrado.");

        var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
        if (doctor == null)
            return (null, "Perfil de doctor no encontrado: tu usuario no tiene uno asociado.");

        var errorTurno = await TurnoVinculado.Revisar(_turnoRepo, dto.IdTurno, dto.IdPaciente, doctor.Id);
        if (errorTurno != null)
            return (null, errorTurno);

        // Si no mandan la fecha del pedido, es hoy.
        var fecha = dto.FechaSolicitud;
        if (fecha == default)
            fecha = _reloj.Ahora().Date;

        var estudio = new Estudio
        {
            IdPaciente     = dto.IdPaciente,
            IdTurno        = dto.IdTurno,
            Tipo           = dto.Tipo,
            Descripcion    = dto.Descripcion.Trim(),
            FechaSolicitud = fecha,
            Estado         = "pendiente"
        };

        await _repo.Agregar(estudio);
        return (estudio.Id, null);
    }

    public async Task<(bool ok, string? error)> CargarResultado(int id, EstudioResultadoDto dto)
    {
        var estudio = await _repo.ObtenerPorId(id);
        if (estudio == null) return (false, "Estudio no encontrado.");

        var estadosValidos = new[] { "validado", "entregado" };
        if (!estadosValidos.Contains(dto.Estado))
            return (false, "Estado inválido. Valores: validado, entregado.");

        estudio.Resultado      = dto.Resultado;
        estudio.ArchivoUrl     = dto.ArchivoUrl;
        estudio.Estado         = dto.Estado;
        estudio.FechaResultado = dto.FechaResultado == default ? _reloj.Ahora() : dto.FechaResultado;

        await _repo.Actualizar(estudio);

        // Un fallo al insertar la notificación nunca puede hacer fracasar ni
        // parecer que fracasó la carga del resultado, que ya se guardó.
        try
        {
            var paciente = await _pacienteRepo.ObtenerPorId(estudio.IdPaciente);
            if (paciente?.Usuario != null)
            {
                await _notifRepo.Agregar(new Notificacion
                {
                    IdUsuario = paciente.Usuario.Id,
                    Tipo      = "estudio",
                    Mensaje   = MensajeDeResultado(estudio),
                    Fecha     = _reloj.Ahora()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo crear la notificación de resultado de estudio para el paciente {IdPaciente}.", estudio.IdPaciente);
        }

        return (true, null);
    }

    /// <summary>
    /// "El resultado de tu estudio «Hemograma completo» ya está disponible."
    /// Si el estudio no tiene descripción, se nombra por el tipo.
    /// </summary>
    public static string MensajeDeResultado(Estudio estudio)
    {
        if (string.IsNullOrWhiteSpace(estudio.Descripcion))
            return $"El resultado de tu estudio de {estudio.Tipo} ya está disponible.";

        return $"El resultado de tu estudio «{estudio.Descripcion.Trim()}» ya está disponible.";
    }

    private static EstudioDto MapearDto(Estudio e) => new EstudioDto(
        e.Id,
        e.Tipo,
        e.Estado,
        e.FechaSolicitud,
        e.IdTurno,
        e.Resultado,
        e.ArchivoUrl,
        e.FechaResultado,
        e.Descripcion,
        Profesional.Armar(e.Turno?.Doctor)
    );
}
