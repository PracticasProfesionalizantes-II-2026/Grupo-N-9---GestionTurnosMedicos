using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IHistorialClinicoRepository
{
    Task<IEnumerable<HistorialClinico>> ObtenerDePaciente(int pacienteId, DateTime? desde, DateTime? hasta);
    Task<HistorialClinico?> ObtenerPorId(int id);
    Task Agregar(HistorialClinico historial);
    Task Actualizar(HistorialClinico historial);
}
