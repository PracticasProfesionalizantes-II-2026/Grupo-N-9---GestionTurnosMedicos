using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class EstudioLogica : IEstudioLogica
{
    private readonly IEstudioRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly INotificacionRepository _notifRepo;
    private readonly ILogger<EstudioLogica> _logger;

    public EstudioLogica(
        IEstudioRepository repo,
        IPacienteRepository pacienteRepo,
        INotificacionRepository notifRepo,
        ILogger<EstudioLogica> logger)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _notifRepo = notifRepo;
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

        var e = await _repo.ObtenerPorId(id);
        if (e == null) return (null, "Estudio no encontrado.", false);

        if (propio != null && propio.Id != e.IdPaciente)
            return (null, null, true);

        return (MapearDto(e), null, false);
    }

    public async Task<(int? id, string? error)> Crear(EstudioCreateDto dto)
    {
        var tiposValidos = new[] { "sangre", "imagen", "biopsia", "otro" };
        if (!tiposValidos.Contains(dto.Tipo))
            return (null, "Tipo inválido. Valores: sangre, imagen, biopsia, otro.");

        var estudio = new Estudio
        {
            IdPaciente     = dto.IdPaciente,
            IdTurno        = dto.IdTurno,
            Tipo           = dto.Tipo,
            Descripcion    = dto.Descripcion,
            FechaSolicitud = dto.FechaSolicitud,
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
        estudio.FechaResultado = dto.FechaResultado;

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
                    Mensaje   = $"El resultado de tu estudio de {estudio.Tipo} ya está disponible."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo crear la notificación de resultado de estudio para el paciente {IdPaciente}.", estudio.IdPaciente);
        }

        return (true, null);
    }

    private static EstudioDto MapearDto(Estudio e) => new EstudioDto(
        e.Id,
        e.Tipo,
        e.Estado,
        e.FechaSolicitud,
        e.IdTurno,
        e.Resultado,
        e.ArchivoUrl,
        e.FechaResultado
    );
}
