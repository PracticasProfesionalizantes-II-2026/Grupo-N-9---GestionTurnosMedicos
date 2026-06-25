using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IRecetaRepository
{
    Task<IEnumerable<Receta>> ObtenerDePaciente(int pacienteId);
    Task<Receta?> ObtenerPorId(int id);
    Task Agregar(Receta receta);
    Task Actualizar(Receta receta);
}
