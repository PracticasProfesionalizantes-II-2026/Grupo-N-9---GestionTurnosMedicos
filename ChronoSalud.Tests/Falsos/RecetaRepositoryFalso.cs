using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a RecetaRepository: las recetas viven en una lista.</summary>
public class RecetaRepositoryFalso : IRecetaRepository
{
    public List<Receta> Recetas { get; } = new List<Receta>();

    public Task<IEnumerable<Receta>> ObtenerDePaciente(int pacienteId)
    {
        var delPaciente = Recetas.Where(r => r.IdPaciente == pacienteId).ToList();
        return Task.FromResult<IEnumerable<Receta>>(delPaciente);
    }

    public Task<Receta?> ObtenerPorId(int id)
    {
        var receta = Recetas.FirstOrDefault(r => r.Id == id);
        return Task.FromResult(receta);
    }

    public Task Agregar(Receta receta)
    {
        receta.Id = Recetas.Count + 1;
        Recetas.Add(receta);
        return Task.CompletedTask;
    }

    public Task Actualizar(Receta receta) => Task.CompletedTask;
}
