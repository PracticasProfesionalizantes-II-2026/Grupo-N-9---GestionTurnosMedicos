using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

/// <summary>Un doctor del listado y si tiene algún día de atención cargado.</summary>
public record DoctorConHorario(Doctor Doctor, bool TieneHorario);

public interface IDoctorRepository
{
    Task<IEnumerable<Doctor>> ObtenerTodos(string? especialidad, int? coberturaId);
    Task<(int total, List<DoctorConHorario> doctores)> BuscarConHorario(string? especialidad, int? coberturaId, int pagina, int limite);
    Task<Doctor?> ObtenerPorId(int id);
    Task<Doctor?> ObtenerPorIdUsuario(int idUsuario);
    Task<bool> ExisteMatricula(string matricula);
    Task Agregar(Doctor doctor);
    Task Actualizar(Doctor doctor);
    Task Eliminar(Doctor doctor);
}
