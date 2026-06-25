using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IPacienteRepository
{
    Task<IEnumerable<Paciente>> ObtenerTodos(string? nombre, int? coberturaId);
    Task<Paciente?> ObtenerPorId(int id);
    Task Actualizar(Paciente paciente);
}
