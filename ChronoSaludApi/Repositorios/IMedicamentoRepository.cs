using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IMedicamentoRepository
{
    Task<IEnumerable<Medicamento>> ObtenerTodos();
    Task<Medicamento?> ObtenerPorId(int id);
    Task Agregar(Medicamento medicamento);
    Task Actualizar(Medicamento medicamento);
    Task Eliminar(Medicamento medicamento);
}
