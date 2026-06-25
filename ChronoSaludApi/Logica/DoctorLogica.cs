using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class DoctorLogica : IDoctorLogica
{
    private readonly IDoctorRepository _repo;

    public DoctorLogica(IDoctorRepository repo) => _repo = repo;

    public async Task<(int total, IEnumerable<DoctorListaDto> doctores)> ObtenerTodos(
        string? especialidad, int? coberturaId, int pagina, int limite)
    {
        var todos = await _repo.ObtenerTodos(especialidad, coberturaId);
        var total = todos.Count();
        var resultado = todos
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .Select(d => new DoctorListaDto(
                d.Id,
                $"{d.Usuario?.Nombre} {d.Usuario?.Apellido}",
                d.Especialidad,
                d.Matricula
            ));
        return (total, resultado);
    }

    public async Task<DoctorDto?> ObtenerPorId(int id)
    {
        var d = await _repo.ObtenerPorId(id);
        if (d == null) return null;

        return new DoctorDto(
            d.Id,
            d.Usuario?.Nombre ?? string.Empty,
            d.Usuario?.Apellido ?? string.Empty,
            d.Especialidad,
            d.Matricula,
            d.Consultorio
        );
    }

    public async Task<(int? id, string? error)> Crear(DoctorCreateDto dto)
    {
        if (await _repo.ExisteMatricula(dto.Matricula))
            return (null, "La matrícula ya se encuentra registrada.");

        var doctor = new Doctor
        {
            IdUsuario   = dto.IdUsuario,
            Especialidad = dto.Especialidad,
            Matricula   = dto.Matricula,
            Consultorio = dto.Consultorio,
            Activo      = true
        };

        await _repo.Agregar(doctor);
        return (doctor.Id, null);
    }

    public async Task<(bool ok, string? error)> Actualizar(int id, DoctorUpdateDto dto)
    {
        var doctor = await _repo.ObtenerPorId(id);
        if (doctor == null) return (false, "Doctor no encontrado.");

        if (!string.IsNullOrEmpty(dto.Especialidad)) doctor.Especialidad = dto.Especialidad;
        if (!string.IsNullOrEmpty(dto.Consultorio))  doctor.Consultorio  = dto.Consultorio;

        await _repo.Actualizar(doctor);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> EliminarLogico(int id)
    {
        var doctor = await _repo.ObtenerPorId(id);
        if (doctor == null) return (false, "Doctor no encontrado.");

        doctor.Activo = false;
        await _repo.Eliminar(doctor);
        return (true, null);
    }
}
