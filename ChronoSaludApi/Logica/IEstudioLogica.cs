using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IEstudioLogica
{
    Task<(IEnumerable<EstudioDto> estudios, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, string? tipo, string? estado, DateTime? desde, int idUsuarioCaller, bool callerEsStaff);
    Task<EstudioDto?> ObtenerPorId(int id);
    Task<(int? id, string? error)> Crear(EstudioCreateDto dto);
    Task<(bool ok, string? error)> CargarResultado(int id, EstudioResultadoDto dto);
}
