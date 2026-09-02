using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _db;

    public DoctorRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Doctor>> ObtenerTodos(string? especialidad, int? coberturaId)
    {
        var query = _db.Doctores.Include(d => d.Usuario).Where(d => d.Activo).AsQueryable();

        if (!string.IsNullOrEmpty(especialidad))
            query = query.Where(d => d.Especialidad.Contains(especialidad));

        return await query.ToListAsync();
    }

    public async Task<Doctor?> ObtenerPorId(int id)
        => await _db.Doctores.Include(d => d.Usuario).FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Doctor?> ObtenerPorIdUsuario(int idUsuario)
        => await _db.Doctores.Include(d => d.Usuario).FirstOrDefaultAsync(d => d.IdUsuario == idUsuario);

    public async Task<bool> ExisteMatricula(string matricula)
        => await _db.Doctores.AnyAsync(d => d.Matricula == matricula);

    public async Task Agregar(Doctor doctor)
    {
        _db.Doctores.Add(doctor);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Doctor doctor)
    {
        _db.Doctores.Update(doctor);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Doctor doctor)
    {
        _db.Doctores.Update(doctor);
        await _db.SaveChangesAsync();
    }
}
