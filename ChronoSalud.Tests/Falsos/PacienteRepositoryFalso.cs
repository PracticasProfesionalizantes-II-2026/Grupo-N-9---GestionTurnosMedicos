using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a PacienteRepository: los pacientes viven en una lista.</summary>
public class PacienteRepositoryFalso : IPacienteRepository
{
    public List<Paciente> Pacientes { get; } = new List<Paciente>();

    public Task<(int total, List<Paciente> pacientes)> Buscar(
        string? buscar, string? nombre, string? dni, int? coberturaId, int pagina, int limite)
    {
        var deLaPagina = Pacientes.Skip((pagina - 1) * limite).Take(limite).ToList();
        return Task.FromResult((Pacientes.Count, deLaPagina));
    }

    public Task<Paciente?> ObtenerPorId(int id)
    {
        var paciente = Pacientes.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(paciente);
    }

    public Task<Paciente?> ObtenerPorIdUsuario(int idUsuario)
    {
        var paciente = Pacientes.FirstOrDefault(p => p.IdUsuario == idUsuario);
        return Task.FromResult(paciente);
    }

    public Task Agregar(Paciente paciente)
    {
        paciente.Id = Pacientes.Count + 1;
        Pacientes.Add(paciente);
        return Task.CompletedTask;
    }

    public Task Actualizar(Paciente paciente) => Task.CompletedTask;

    public Task<bool> ExisteDniEnOtroPaciente(string dni, int idPacienteExcluir)
    {
        var existe = Pacientes.Any(p => p.Dni == dni && p.Id != idPacienteExcluir);
        return Task.FromResult(existe);
    }
}
