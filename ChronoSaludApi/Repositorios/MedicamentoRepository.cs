using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class MedicamentoRepository : IMedicamentoRepository
{
    private readonly AppDbContext _db;

    public MedicamentoRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Medicamento>> ObtenerTodos()
        => await _db.Medicamentos.ToListAsync();

    public async Task<Medicamento?> ObtenerPorId(int id)
        => await _db.Medicamentos.FindAsync(id);

    public async Task Agregar(Medicamento medicamento)
    {
        _db.Medicamentos.Add(medicamento);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Medicamento medicamento)
    {
        _db.Medicamentos.Update(medicamento);
        await _db.SaveChangesAsync();
    }

    public async Task Eliminar(Medicamento medicamento)
    {
        _db.Medicamentos.Remove(medicamento);
        await _db.SaveChangesAsync();
    }
}
