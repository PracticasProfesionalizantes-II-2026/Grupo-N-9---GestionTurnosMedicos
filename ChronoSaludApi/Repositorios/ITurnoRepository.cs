using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface ITurnoRepository
{
    Task<IEnumerable<Turno>> ObtenerTodos(int? pacienteId, int? doctorId, string? estado, DateTime? desde, DateTime? hasta);
    Task<(int total, List<Turno> turnos)> Buscar(FiltroTurnos filtro, string orden, bool descendente, int pagina, int limite);
    Task<Dictionary<string, int>> ContarPorEstado(FiltroTurnos filtro);
    Task<int> ContarPacientesAtendidos(DateTime desde, DateTime hasta);
    Task<Dictionary<int, int>> ContarOcupadosPorDoctor(int? doctorId, DateTime desde, DateTime hasta);
    Task<Turno?> ObtenerPorId(int id);
    Task<bool> HayConflictoHorario(int doctorId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin, int? turnoId = null);
    Task Agregar(Turno turno);
    Task Actualizar(Turno turno);
    Task Eliminar(Turno turno);
}
