using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class PacienteRepository : IPacienteRepository
{
    private readonly AppDbContext _db;

    public PacienteRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Paciente>> ObtenerTodos(string? nombre, int? coberturaId)
    {
        var query = _db.Pacientes.Include(p => p.Usuario).AsQueryable();

        if (!string.IsNullOrEmpty(nombre))
            query = query.Where(p =>
                p.Usuario!.Nombre.Contains(nombre) ||
                p.Usuario!.Apellido.Contains(nombre));

        if (coberturaId.HasValue)
            query = query.Where(p =>
                p.PacienteCoberturas.Any(pc => pc.IdCobertura == coberturaId));

        return await query.ToListAsync();
    }

    public async Task<Paciente?> ObtenerPorId(int id)
        => await _db.Pacientes.Include(p => p.Usuario).FirstOrDefaultAsync(p => p.Id == id);

    public async Task Actualizar(Paciente paciente)
    {
        _db.Pacientes.Update(paciente);
        await _db.SaveChangesAsync();
    }
}
