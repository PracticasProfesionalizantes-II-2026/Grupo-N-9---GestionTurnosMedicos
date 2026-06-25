using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class EstudioRepository : IEstudioRepository
{
    private readonly AppDbContext _db;

    public EstudioRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Estudio>> ObtenerDePaciente(int pacienteId, string? tipo, string? estado, DateTime? desde)
    {
        var query = _db.Estudios.Where(e => e.IdPaciente == pacienteId).AsQueryable();
        if (!string.IsNullOrEmpty(tipo))   query = query.Where(e => e.Tipo == tipo);
        if (!string.IsNullOrEmpty(estado)) query = query.Where(e => e.Estado == estado);
        if (desde.HasValue)                query = query.Where(e => e.FechaSolicitud >= desde);
        return await query.OrderByDescending(e => e.FechaSolicitud).ToListAsync();
    }

    public async Task<Estudio?> ObtenerPorId(int id)
        => await _db.Estudios.FindAsync(id);

    public async Task Agregar(Estudio estudio)
    {
        _db.Estudios.Add(estudio);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Estudio estudio)
    {
        _db.Estudios.Update(estudio);
        await _db.SaveChangesAsync();
    }
}
