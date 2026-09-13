using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IPacienteLogica
{
    Task<(int total, IEnumerable<PacienteListaDto> pacientes)> ObtenerTodos(string? nombre, int? coberturaId, int pagina, int limite);
    Task<(PacienteDto? paciente, string? error, bool prohibido)> ObtenerPorId(int id, int idUsuarioCaller, bool callerEsStaff);
    Task<PacienteDto?> ObtenerPorIdUsuario(int idUsuario);
    Task<(bool ok, string? error, bool prohibido)> Actualizar(int id, PacienteUpdateDto dto, int idUsuarioCaller, bool callerEsStaff);
}
