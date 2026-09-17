using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IRecetaLogica
{
    Task<(IEnumerable<RecetaDto> recetas, string? error, bool prohibido)> ObtenerDePaciente(int pacienteId, int idUsuarioCaller, bool callerEsStaff);
    Task<(RecetaDto? receta, string? error, bool prohibido)> ObtenerPorId(int id, int idUsuarioCaller, bool callerEsStaff);
    Task<(int? id, string? error)> Crear(RecetaCreateDto dto);
    Task<(bool ok, string? error)> Actualizar(int id, RecetaCreateDto dto);
}
