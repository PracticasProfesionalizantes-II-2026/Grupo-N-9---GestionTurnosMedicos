using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a HistorialClinicoRepository: las entradas viven en una lista.</summary>
public class HistorialClinicoRepositoryFalso : IHistorialClinicoRepository
{
    public List<HistorialClinico> Entradas { get; } = new List<HistorialClinico>();

    public Task<IEnumerable<HistorialClinico>> ObtenerDePaciente(int pacienteId, DateTime? desde, DateTime? hasta)
    {
        var delPaciente = Entradas.Where(h => h.IdPaciente == pacienteId).ToList();
        return Task.FromResult<IEnumerable<HistorialClinico>>(delPaciente);
    }

    public Task<HistorialClinico?> ObtenerPorId(int id)
    {
        var entrada = Entradas.FirstOrDefault(h => h.Id == id);
        return Task.FromResult(entrada);
    }

    public Task Agregar(HistorialClinico historial)
    {
        historial.Id = Entradas.Count + 1;
        Entradas.Add(historial);
        return Task.CompletedTask;
    }

    public Task Actualizar(HistorialClinico historial) => Task.CompletedTask;
}
