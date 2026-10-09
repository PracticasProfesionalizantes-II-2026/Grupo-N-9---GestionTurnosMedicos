using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class RecetaRepository : IRecetaRepository
{
    private readonly AppDbContext _db;

    public RecetaRepository(AppDbContext db) => _db = db;

    // El doctor y su cuenta se cargan para mostrar quién firmó la receta. El
    // medicamento, para las recetas viejas que no tienen la copia de sus datos.
    public async Task<IEnumerable<Receta>> ObtenerDePaciente(int pacienteId)
        => await _db.Recetas
            .Include(r => r.RecetaMedicamentos)
                .ThenInclude(rm => rm.Medicamento)
            .Include(r => r.Doctor).ThenInclude(d => d!.Usuario)
            .Where(r => r.IdPaciente == pacienteId)
            .OrderByDescending(r => r.Fecha)
            .ToListAsync();

    public async Task<Receta?> ObtenerPorId(int id)
        => await _db.Recetas
            .Include(r => r.RecetaMedicamentos)
                .ThenInclude(rm => rm.Medicamento)
            .Include(r => r.Doctor).ThenInclude(d => d!.Usuario)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task Agregar(Receta receta)
    {
        _db.Recetas.Add(receta);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Receta receta)
    {
        _db.Recetas.Update(receta);
        await _db.SaveChangesAsync();
    }
}
