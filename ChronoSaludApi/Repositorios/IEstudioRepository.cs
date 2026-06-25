using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IEstudioRepository
{
    Task<IEnumerable<Estudio>> ObtenerDePaciente(int pacienteId, string? tipo, string? estado, DateTime? desde);
    Task<Estudio?> ObtenerPorId(int id);
    Task Agregar(Estudio estudio);
    Task Actualizar(Estudio estudio);
}
