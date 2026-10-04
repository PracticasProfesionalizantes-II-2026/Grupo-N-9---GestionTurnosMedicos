using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class UsuarioFotoRepository : IUsuarioFotoRepository
{
    private readonly AppDbContext _db;

    public UsuarioFotoRepository(AppDbContext db) => _db = db;

    public async Task<UsuarioFoto?> ObtenerPorIdUsuario(int idUsuario)
        => await _db.UsuarioFotos.AsNoTracking().FirstOrDefaultAsync(f => f.IdUsuario == idUsuario);

    // Solo la fecha, sin traer el contenido: alcanza para saber si la foto
    // cambió desde la última vez que se la pidió.
    public async Task<DateTime?> ObtenerFechaActualizacion(int idUsuario)
        => await _db.UsuarioFotos
            .Where(f => f.IdUsuario == idUsuario)
            .Select(f => (DateTime?)f.ActualizadaEn)
            .FirstOrDefaultAsync();

    // Crea la foto o reemplaza la que ya tenía el usuario.
    public async Task Guardar(UsuarioFoto foto)
    {
        var existente = await _db.UsuarioFotos.FindAsync(foto.IdUsuario);
        if (existente == null)
        {
            _db.UsuarioFotos.Add(foto);
        }
        else
        {
            existente.Contenido     = foto.Contenido;
            existente.TipoContenido = foto.TipoContenido;
            existente.ActualizadaEn = foto.ActualizadaEn;
        }

        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(int idUsuario)
        => await _db.UsuarioFotos.Where(f => f.IdUsuario == idUsuario).ExecuteDeleteAsync();

    // Rol del dueño y fecha de la foto, en una sola consulta. Null si no tiene foto.
    public async Task<FotoAcceso?> ObtenerAcceso(int idUsuario)
        => await (
            from f in _db.UsuarioFotos
            join u in _db.Usuarios on f.IdUsuario equals u.Id
            where f.IdUsuario == idUsuario
            select new FotoAcceso(u.Rol, f.ActualizadaEn)
        ).FirstOrDefaultAsync();

    // De los pacientes pedidos, los que tienen foto.
    public async Task<IEnumerable<FotoDueno>> ObtenerDePacientes(IEnumerable<int> idsPaciente)
    {
        var ids = idsPaciente.ToList();
        return await (
            from p in _db.Pacientes
            join u in _db.Usuarios on p.IdUsuario equals u.Id
            join f in _db.UsuarioFotos on u.Id equals f.IdUsuario
            where ids.Contains(p.Id)
            select new FotoDueno(p.Id, u.Id, u.Rol)
        ).ToListAsync();
    }

    // De los doctores pedidos, los que tienen foto.
    public async Task<IEnumerable<FotoDueno>> ObtenerDeDoctores(IEnumerable<int> idsDoctor)
    {
        var ids = idsDoctor.ToList();
        return await (
            from d in _db.Doctores
            join u in _db.Usuarios on d.IdUsuario equals u.Id
            join f in _db.UsuarioFotos on u.Id equals f.IdUsuario
            where ids.Contains(d.Id)
            select new FotoDueno(d.Id, u.Id, u.Rol)
        ).ToListAsync();
    }

    // De los turnos pedidos, aquellos cuyo paciente tiene foto.
    public async Task<IEnumerable<FotoDueno>> ObtenerDeTurnos(IEnumerable<int> idsTurno)
    {
        var ids = idsTurno.ToList();
        return await (
            from t in _db.Turnos
            join p in _db.Pacientes on t.IdPaciente equals p.Id
            join u in _db.Usuarios on p.IdUsuario equals u.Id
            join f in _db.UsuarioFotos on u.Id equals f.IdUsuario
            where ids.Contains(t.Id)
            select new FotoDueno(t.Id, u.Id, u.Rol)
        ).ToListAsync();
    }
}
