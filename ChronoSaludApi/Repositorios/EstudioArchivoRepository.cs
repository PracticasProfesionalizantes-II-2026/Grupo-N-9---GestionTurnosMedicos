using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class EstudioArchivoRepository : IEstudioArchivoRepository
{
    private readonly AppDbContext _db;

    public EstudioArchivoRepository(AppDbContext db) => _db = db;

    public async Task<EstudioArchivo?> Obtener(int idEstudio)
        => await _db.EstudioArchivos.AsNoTracking().FirstOrDefaultAsync(a => a.IdEstudio == idEstudio);

    // Una sola consulta con el id y el nombre: el contenido no se trae.
    public async Task<Dictionary<int, string>> NombresDe(IEnumerable<int> idsEstudio)
    {
        var ids = idsEstudio.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();

        return await _db.EstudioArchivos
            .Where(a => ids.Contains(a.IdEstudio))
            .Select(a => new { a.IdEstudio, a.Nombre })
            .ToDictionaryAsync(a => a.IdEstudio, a => a.Nombre);
    }

    public async Task Guardar(EstudioArchivo archivo)
    {
        var existente = await _db.EstudioArchivos.FindAsync(archivo.IdEstudio);
        if (existente == null)
        {
            _db.EstudioArchivos.Add(archivo);
        }
        else
        {
            existente.Contenido = archivo.Contenido;
            existente.Nombre    = archivo.Nombre;
            existente.Tamano    = archivo.Tamano;
            existente.SubidoEn  = archivo.SubidoEn;
        }

        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(int idEstudio)
        => await _db.EstudioArchivos.Where(a => a.IdEstudio == idEstudio).ExecuteDeleteAsync();
}
