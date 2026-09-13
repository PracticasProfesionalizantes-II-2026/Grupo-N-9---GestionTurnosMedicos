using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface ITurnoLogica
{
    Task<(int total, IEnumerable<TurnoListaDto> turnos, string? error)> ObtenerTodos(
        int? pacienteId, int? doctorId, string? estado, DateTime? desde, DateTime? hasta, int pagina, int limite,
        int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor);
    Task<(TurnoDto? turno, string? error)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor, bool callerEsStaff);
    Task<(int? id, string? error)> Crear(TurnoCreateDto dto);
    Task<(bool ok, string? error)> Actualizar(int id, TurnoUpdateDto dto);
    Task<(bool ok, string? error)> Cancelar(int id);
}
