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

    public async Task<bool> HayAdministradorActivo()
        => await _db.Usuarios.AnyAsync(u => u.Rol == "administrador" && u.Activo);

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

    // Solo usuarios activos. El total y la página se resuelven en SQL; se
    // incluyen Paciente y Doctor para poder informar sus ids.
    public async Task<(int total, IEnumerable<Usuario> usuarios)> Buscar(
        string? buscar, string? rol, int pagina, int limite)
    {
        var query = _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Paciente)
            .Include(u => u.Doctor)
            .Where(u => u.Activo);

        if (!string.IsNullOrEmpty(buscar))
            query = query.Where(u =>
                u.Nombre.Contains(buscar) ||
                u.Apellido.Contains(buscar) ||
                u.Email.Contains(buscar));

        if (!string.IsNullOrEmpty(rol))
            query = query.Where(u => u.Rol == rol);

        var total = await query.CountAsync();

        var usuarios = await query
            .OrderBy(u => u.Apellido)
            .ThenBy(u => u.Nombre)
            .ThenBy(u => u.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (total, usuarios);
    }

    // De los usuarios pedidos, los que tienen foto. Solo consulta los ids:
    // no trae el contenido de ninguna.
    public async Task<IEnumerable<int>> ObtenerIdsConFoto(IEnumerable<int> idsUsuario)
    {
        var ids = idsUsuario.ToList();
        return await _db.UsuarioFotos
            .Where(f => ids.Contains(f.IdUsuario))
            .Select(f => f.IdUsuario)
            .ToListAsync();
    }
}
