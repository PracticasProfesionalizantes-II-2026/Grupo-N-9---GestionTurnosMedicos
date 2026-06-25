using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _db;

    public UsuarioRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Usuario>> ObtenerTodos()
        => await _db.Usuarios.Where(u => u.Activo).ToListAsync();

    public async Task<Usuario?> ObtenerPorId(int id)
        => await _db.Usuarios.FindAsync(id);

    public async Task<Usuario?> ObtenerPorEmail(string email)
        => await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

    public async Task Agregar(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Usuario usuario)
    {
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Usuario usuario)
    {
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
    }
}
