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

    // Una página del listado, con si cada doctor tiene horario. Eso sale de
    // una subconsulta que devuelve solo el booleano: los horarios no se traen.
    // El total, el orden y la página los resuelve SQL Server.
    public async Task<(int total, List<DoctorConHorario> doctores)> BuscarConHorario(
        string? especialidad, int? coberturaId, int pagina, int limite)
    {
        var query = _db.Doctores.Where(d => d.Activo);

        if (!string.IsNullOrEmpty(especialidad))
            query = query.Where(d => d.Especialidad.Contains(especialidad));

        var total = await query.CountAsync();

        var filas = await query
            .OrderBy(d => d.Usuario!.Apellido)
            .ThenBy(d => d.Usuario!.Nombre)
            .ThenBy(d => d.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .Select(d => new { Doctor = d, d.Usuario, TieneHorario = d.HorariosLaborales.Any() })
            .ToListAsync();

        var doctores = new List<DoctorConHorario>();
        foreach (var fila in filas)
        {
            // Con un Select, EF no completa la navegación: se la asigna a mano.
            fila.Doctor.Usuario ??= fila.Usuario;
            doctores.Add(new DoctorConHorario(fila.Doctor, fila.TieneHorario));
        }

        return (total, doctores);
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
