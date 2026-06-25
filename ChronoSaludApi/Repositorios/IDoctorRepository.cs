using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IDoctorRepository
{
    Task<IEnumerable<Doctor>> ObtenerTodos(string? especialidad, int? coberturaId);
    Task<Doctor?> ObtenerPorId(int id);
    Task<bool> ExisteMatricula(string matricula);
    Task Agregar(Doctor doctor);
    Task Actualizar(Doctor doctor);
    Task Eliminar(Doctor doctor);
}
