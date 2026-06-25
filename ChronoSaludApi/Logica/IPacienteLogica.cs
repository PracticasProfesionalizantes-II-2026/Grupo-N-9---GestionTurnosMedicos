using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IPacienteLogica
{
    Task<(int total, IEnumerable<PacienteListaDto> pacientes)> ObtenerTodos(string? nombre, int? coberturaId, int pagina, int limite);
    Task<PacienteDto?> ObtenerPorId(int id);
    Task<(bool ok, string? error)> Actualizar(int id, PacienteUpdateDto dto);
}
