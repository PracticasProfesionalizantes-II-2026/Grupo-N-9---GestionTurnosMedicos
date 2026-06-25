using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class EstudioLogica : IEstudioLogica
{
    private readonly IEstudioRepository _repo;

    public EstudioLogica(IEstudioRepository repo) => _repo = repo;

    public async Task<IEnumerable<EstudioDto>> ObtenerDePaciente(
        int pacienteId, string? tipo, string? estado, DateTime? desde)
    {
        var estudios = await _repo.ObtenerDePaciente(pacienteId, tipo, estado, desde);
        return estudios.Select(MapearDto);
    }

    public async Task<EstudioDto?> ObtenerPorId(int id)
    {
        var e = await _repo.ObtenerPorId(id);
        return e == null ? null : MapearDto(e);
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
