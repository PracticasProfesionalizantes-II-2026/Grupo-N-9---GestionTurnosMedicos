using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface ITurnoLogica
{
    Task<(int total, IEnumerable<TurnoListaDto> turnos)> ObtenerTodos(int? pacienteId, int? doctorId, string? estado, DateTime? desde, DateTime? hasta, int pagina, int limite);
    Task<TurnoDto?> ObtenerPorId(int id);
    Task<(int? id, string? error)> Crear(TurnoCreateDto dto);
    Task<(bool ok, string? error)> Actualizar(int id, TurnoUpdateDto dto);
    Task<(bool ok, string? error)> Cancelar(int id);
}
