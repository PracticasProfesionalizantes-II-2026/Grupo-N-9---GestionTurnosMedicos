using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface ICoberturaRepository
{
    Task<IEnumerable<Cobertura>> ObtenerTodas(string? nombre);
    Task<Cobertura?> ObtenerPorId(int id);
    Task<IEnumerable<PacienteCobertura>> ObtenerCoberturasDePaciente(int pacienteId);
    Task<PacienteCobertura?> ObtenerPacienteCobertura(int pacienteId, int coberturaId);
    Task AgregarPacienteCobertura(PacienteCobertura pc);
    Task ActualizarPacienteCobertura(PacienteCobertura pc);
    Task EliminarPacienteCobertura(PacienteCobertura pc);
}
