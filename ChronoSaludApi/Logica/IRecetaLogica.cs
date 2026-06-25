using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IRecetaLogica
{
    Task<IEnumerable<RecetaDto>> ObtenerDePaciente(int pacienteId);
    Task<RecetaDto?> ObtenerPorId(int id);
    Task<(int? id, string? error)> Crear(RecetaCreateDto dto);
    Task<(bool ok, string? error)> Actualizar(int id, RecetaCreateDto dto);
}
