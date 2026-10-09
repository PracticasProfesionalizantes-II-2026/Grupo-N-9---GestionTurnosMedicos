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

    // La cuenta con su perfil de doctor (si tiene): la baja y la reactivación
    // cambian los dos.
    public async Task<Usuario?> ObtenerConDoctor(int id)
        => await _db.Usuarios.Include(u => u.Doctor).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Usuario?> ObtenerPorEmail(string email)
        => await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> HayAdministradorActivo()
        => await _db.Usuarios.AnyAsync(u => u.Rol == "administrador" && u.Activo);

    public async Task<int> ContarAdministradoresActivos()
        => await _db.Usuarios.CountAsync(u => u.Rol == "administrador" && u.Activo);

    // Se usa en cada pedido con token (Program.cs): es una consulta chica, sin
    // traer la fila entera.
    public async Task<bool> EstaActivo(int id)
        => await _db.Usuarios.AnyAsync(u => u.Id == id && u.Activo);

    // Si el usuario trae su Paciente cargado, se guardan los dos juntos.
    public async Task Agregar(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (ErroresDeBase.EsDatoRepetido(error))
        {
            // Otro pedido guardó el mismo email o DNI un instante antes. Se
            // limpia el contexto para que nada quede a medio guardar.
            _db.ChangeTracker.Clear();
            throw new DatoRepetidoException(error);
        }
    }

    public async Task Actualizar(Usuario usuario)
    {
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
    }

    // Baja o reactivación: guarda solo el campo Activo de la cuenta y, si
    // tiene, el de su perfil de doctor. Van en un solo SaveChanges, así se
    // guardan los dos juntos o ninguno.
    public async Task GuardarActivo(Usuario usuario)
    {
        _db.Entry(usuario).Property(u => u.Activo).IsModified = true;
        if (usuario.Doctor != null)
            _db.Entry(usuario.Doctor).Property(d => d.Activo).IsModified = true;

        await _db.SaveChangesAsync();
    }

    // Solo usuarios activos o, con bajas en true, solo los dados de baja. El
    // total y la página se resuelven en SQL; se incluyen Paciente y Doctor
    // para poder informar sus ids.
    public async Task<(int total, IEnumerable<Usuario> usuarios)> Buscar(
        string? buscar, string? rol, int pagina, int limite, bool bajas = false)
    {
        var query = _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Paciente)
            .Include(u => u.Doctor)
            .AsQueryable();

        if (bajas)
            query = query.Where(u => !u.Activo);
        else
            query = query.Where(u => u.Activo);

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
