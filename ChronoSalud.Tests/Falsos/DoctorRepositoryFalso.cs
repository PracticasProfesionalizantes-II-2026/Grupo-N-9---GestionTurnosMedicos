using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a DoctorRepository: los doctores viven en una lista.</summary>
public class DoctorRepositoryFalso : IDoctorRepository
{
    public List<Doctor> Doctores { get; } = new List<Doctor>();

    public Task<IEnumerable<Doctor>> ObtenerTodos(string? especialidad, int? coberturaId)
    {
        var activos = Doctores.Where(d => d.Activo).ToList();
        return Task.FromResult<IEnumerable<Doctor>>(activos);
    }

    public Task<IEnumerable<DoctorConHorario>> ObtenerTodosConHorario(string? especialidad, int? coberturaId)
    {
        var resultado = new List<DoctorConHorario>();

        foreach (var doctor in Doctores)
        {
            if (doctor.Activo)
                resultado.Add(new DoctorConHorario(doctor, doctor.HorariosLaborales.Count > 0));
        }

        return Task.FromResult<IEnumerable<DoctorConHorario>>(resultado);
    }

    public Task<Doctor?> ObtenerPorId(int id)
    {
        var doctor = Doctores.FirstOrDefault(d => d.Id == id);
        return Task.FromResult(doctor);
    }

    public Task<Doctor?> ObtenerPorIdUsuario(int idUsuario)
    {
        var doctor = Doctores.FirstOrDefault(d => d.IdUsuario == idUsuario);
        return Task.FromResult(doctor);
    }

    public Task<bool> ExisteMatricula(string matricula)
    {
        var existe = Doctores.Any(d => d.Matricula == matricula);
        return Task.FromResult(existe);
    }

    public Task Agregar(Doctor doctor)
    {
        doctor.Id = Doctores.Count + 1;
        Doctores.Add(doctor);
        return Task.CompletedTask;
    }

    public Task Actualizar(Doctor doctor) => Task.CompletedTask;

    public Task Eliminar(Doctor doctor) => Task.CompletedTask;
}
