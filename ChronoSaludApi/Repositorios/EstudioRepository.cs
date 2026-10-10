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
        // Con el turno y su doctor, para mostrar quién pidió cada estudio.
        var query = _db.Estudios
            .Include(e => e.Turno).ThenInclude(t => t!.Doctor).ThenInclude(d => d!.Usuario)
            .Where(e => e.IdPaciente == pacienteId)
            .AsQueryable();
        if (!string.IsNullOrEmpty(tipo))   query = query.Where(e => e.Tipo == tipo);
        if (!string.IsNullOrEmpty(estado)) query = query.Where(e => e.Estado == estado);
        if (desde.HasValue)                query = query.Where(e => e.FechaSolicitud >= desde);
        return await query.OrderByDescending(e => e.FechaSolicitud).ToListAsync();
    }

    public async Task<Estudio?> ObtenerPorId(int id)
        => await _db.Estudios.FindAsync(id);

    // Solo para leer: si se actualizara con todo esto cargado, Update marcaría
    // también el turno y el doctor como modificados.
    public async Task<Estudio?> ObtenerConDoctor(int id)
        => await _db.Estudios
            .AsNoTracking()
            .Include(e => e.Turno).ThenInclude(t => t!.Doctor).ThenInclude(d => d!.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id);

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
