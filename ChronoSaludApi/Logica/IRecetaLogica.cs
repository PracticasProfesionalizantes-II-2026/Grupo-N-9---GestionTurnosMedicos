using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IRecetaLogica
{
    Task<(IEnumerable<RecetaDto> recetas, string? error, bool prohibido)> ObtenerDePaciente(int pacienteId, int idUsuarioCaller, bool callerEsStaff);
    Task<(RecetaDto? receta, string? error, bool prohibido)> ObtenerPorId(int id, int idUsuarioCaller, bool callerEsStaff);
    Task<(int? id, string? error, bool sinPerfilDoctor)> Crear(RecetaCreateDto dto, int idUsuarioCaller, bool callerEsDoctor);
    Task<(bool ok, string? error, bool sinPerfilDoctor)> Actualizar(int id, RecetaCreateDto dto, int idUsuarioCaller, bool callerEsDoctor);
}
