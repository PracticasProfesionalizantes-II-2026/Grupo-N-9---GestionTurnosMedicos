using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IPacienteRepository
{
    Task<IEnumerable<Paciente>> ObtenerTodos(string? nombre, string? dni, int? coberturaId);
    Task<Paciente?> ObtenerPorId(int id);
    Task<Paciente?> ObtenerPorIdUsuario(int idUsuario);
    Task Agregar(Paciente paciente);
    Task Actualizar(Paciente paciente);
    Task<bool> ExisteDniEnOtroPaciente(string dni, int idPacienteExcluir);
}
