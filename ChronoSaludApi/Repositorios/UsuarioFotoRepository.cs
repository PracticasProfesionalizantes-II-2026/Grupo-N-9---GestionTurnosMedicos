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
}
