using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a EstudioRepository: guarda los estudios en una lista.</summary>
public class EstudioRepositoryFalso : IEstudioRepository
{
    public List<Estudio> Estudios { get; } = new List<Estudio>();

    public Task<IEnumerable<Estudio>> ObtenerDePaciente(int pacienteId, string? tipo, string? estado, DateTime? desde)
    {
        var delPaciente = Estudios.Where(e => e.IdPaciente == pacienteId).ToList();
        return Task.FromResult<IEnumerable<Estudio>>(delPaciente);
    }

    public Task<Estudio?> ObtenerPorId(int id)
        => Task.FromResult(Estudios.FirstOrDefault(e => e.Id == id));

    public Task<Estudio?> ObtenerConDoctor(int id)
        => Task.FromResult(Estudios.FirstOrDefault(e => e.Id == id));

    public Task Agregar(Estudio estudio)
    {
        estudio.Id = Estudios.Count + 1;
        Estudios.Add(estudio);
        return Task.CompletedTask;
    }

    public Task Actualizar(Estudio estudio) => Task.CompletedTask;

    public Task<int> MarcarVistos(int idPaciente)
    {
        var vistos = Estudios.Where(e => e.IdPaciente == idPaciente && e.Estado == "validado").ToList();
        vistos.ForEach(e => e.Estado = "entregado");
        return Task.FromResult(vistos.Count);
    }

    public Task MarcarVisto(int idEstudio)
    {
        foreach (var estudio in Estudios.Where(e => e.Id == idEstudio && e.Estado == "validado"))
            estudio.Estado = "entregado";
        return Task.CompletedTask;
    }
}
