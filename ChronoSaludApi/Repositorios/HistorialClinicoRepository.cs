using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class HistorialClinicoRepository : IHistorialClinicoRepository
{
    private readonly AppDbContext _db;

    public HistorialClinicoRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<HistorialClinico>> ObtenerDePaciente(int pacienteId, DateTime? desde, DateTime? hasta)
    {
        var query = _db.HistorialesClinicos.Where(h => h.IdPaciente == pacienteId).AsQueryable();
        if (desde.HasValue) query = query.Where(h => h.Fecha >= desde);
        if (hasta.HasValue) query = query.Where(h => h.Fecha <= hasta);
        return await query.OrderByDescending(h => h.Fecha).ToListAsync();
    }

    public async Task<HistorialClinico?> ObtenerPorId(int id)
        => await _db.HistorialesClinicos.FindAsync(id);

    public async Task Agregar(HistorialClinico historial)
    {
        _db.HistorialesClinicos.Add(historial);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(HistorialClinico historial)
    {
        _db.HistorialesClinicos.Update(historial);
        await _db.SaveChangesAsync();
    }
}
