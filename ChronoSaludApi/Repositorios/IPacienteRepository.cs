using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IPacienteRepository
{
    Task<(int total, List<Paciente> pacientes)> Buscar(string? nombre, string? dni, int? coberturaId, int pagina, int limite);
    Task<Paciente?> ObtenerPorId(int id);
    Task<Paciente?> ObtenerPorIdUsuario(int idUsuario);
    Task Agregar(Paciente paciente);
    Task Actualizar(Paciente paciente);
    Task<bool> ExisteDniEnOtroPaciente(string dni, int idPacienteExcluir);
}
