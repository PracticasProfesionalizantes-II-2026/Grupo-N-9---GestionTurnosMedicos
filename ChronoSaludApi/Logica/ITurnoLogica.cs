using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public interface ITurnoLogica
{
    Task<(int total, IEnumerable<TurnoListaDto> turnos, Dictionary<string, int> conteos, string? error)> ObtenerTodos(
        FiltroTurnos filtro, string orden, bool descendente, int pagina, int limite,
        int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor);
    Task<(TurnoDto? turno, string? error)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsPaciente, bool callerEsDoctor, bool callerEsStaff);
    Task<(int? id, string? error)> Crear(TurnoCreateDto dto, Solicitante solicitante);
    Task<(bool ok, string? error, bool sinPerfilDoctor)> Actualizar(
        int id, TurnoUpdateDto dto, Solicitante solicitante);
    Task<(bool ok, string? error)> Cancelar(int id, Solicitante solicitante);
}
