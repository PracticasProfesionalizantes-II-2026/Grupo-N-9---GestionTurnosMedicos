using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class RecetaRepository : IRecetaRepository
{
    private readonly AppDbContext _db;

    public RecetaRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Receta>> ObtenerDePaciente(int pacienteId)
        => await _db.Recetas
            .Include(r => r.RecetaMedicamentos)
                .ThenInclude(rm => rm.Medicamento)
            .Where(r => r.IdPaciente == pacienteId)
            .OrderByDescending(r => r.Fecha)
            .ToListAsync();

    public async Task<Receta?> ObtenerPorId(int id)
        => await _db.Recetas
            .Include(r => r.RecetaMedicamentos)
                .ThenInclude(rm => rm.Medicamento)
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
