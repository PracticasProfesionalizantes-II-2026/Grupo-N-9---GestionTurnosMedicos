using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a MedicamentoRepository: los medicamentos viven en una lista.</summary>
public class MedicamentoRepositoryFalso : IMedicamentoRepository
{
    public List<Medicamento> Medicamentos { get; } = new List<Medicamento>();

    /// <summary>Ids de los medicamentos que figuran en alguna receta.</summary>
    public HashSet<int> Recetados { get; } = new HashSet<int>();

    public Task<IEnumerable<Medicamento>> ObtenerTodos()
        => Task.FromResult<IEnumerable<Medicamento>>(Medicamentos.ToList());

    public Task<Medicamento?> ObtenerPorId(int id)
    {
        var medicamento = Medicamentos.FirstOrDefault(m => m.Id == id);
        return Task.FromResult(medicamento);
    }

    public Task Agregar(Medicamento medicamento)
    {
        medicamento.Id = Medicamentos.Count + 1;
        Medicamentos.Add(medicamento);
        return Task.CompletedTask;
    }

    public Task Actualizar(Medicamento medicamento) => Task.CompletedTask;

    public Task Eliminar(Medicamento medicamento)
    {
        Medicamentos.Remove(medicamento);
        return Task.CompletedTask;
    }

    public Task<bool> EstaEnAlgunaReceta(int id) => Task.FromResult(Recetados.Contains(id));
}
