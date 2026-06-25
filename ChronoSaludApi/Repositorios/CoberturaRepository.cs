using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class CoberturaRepository : ICoberturaRepository
{
    private readonly AppDbContext _db;

    public CoberturaRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Cobertura>> ObtenerTodas(string? nombre)
    {
        var query = _db.Coberturas.AsQueryable();
        if (!string.IsNullOrEmpty(nombre))
            query = query.Where(c => c.Nombre.Contains(nombre));
        return await query.ToListAsync();
    }

    public async Task<Cobertura?> ObtenerPorId(int id)
        => await _db.Coberturas.FindAsync(id);

    public async Task<IEnumerable<PacienteCobertura>> ObtenerCoberturasDePaciente(int pacienteId)
        => await _db.PacienteCoberturas
            .Include(pc => pc.Cobertura)
            .Where(pc => pc.IdPaciente == pacienteId)
            .ToListAsync();

    public async Task<PacienteCobertura?> ObtenerPacienteCobertura(int pacienteId, int coberturaId)
        => await _db.PacienteCoberturas
            .FirstOrDefaultAsync(pc => pc.IdPaciente == pacienteId && pc.IdCobertura == coberturaId);

    public async Task AgregarPacienteCobertura(PacienteCobertura pc)
    {
        _db.PacienteCoberturas.Add(pc);
        await _db.SaveChangesAsync();
    }

    public async Task ActualizarPacienteCobertura(PacienteCobertura pc)
    {
        _db.PacienteCoberturas.Update(pc);
        await _db.SaveChangesAsync();
    }

    public async Task EliminarPacienteCobertura(PacienteCobertura pc)
    {
        _db.PacienteCoberturas.Remove(pc);
        await _db.SaveChangesAsync();
    }
}
