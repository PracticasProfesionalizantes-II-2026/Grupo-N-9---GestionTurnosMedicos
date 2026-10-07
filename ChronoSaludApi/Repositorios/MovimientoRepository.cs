using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class MovimientoRepository : IMovimientoRepository
{
    private readonly AppDbContext _db;

    public MovimientoRepository(AppDbContext db) => _db = db;

    public async Task Agregar(Movimiento movimiento)
    {
        _db.Movimientos.Add(movimiento);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Si no se pudo escribir, la fila no tiene que quedar pendiente en
            // el contexto: viajaría en el próximo guardado del mismo pedido.
            _db.Entry(movimiento).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<(int total, IReadOnlyList<Movimiento> movimientos)> Buscar(
        int? idDoctor, string? accion, DateTime? desdeUtc, DateTime? antesDeUtc, int pagina, int limite)
    {
        var query = _db.Movimientos.AsNoTracking();

        if (idDoctor.HasValue)             query = query.Where(m => m.IdDoctor == idDoctor);
        if (!string.IsNullOrEmpty(accion)) query = query.Where(m => m.Accion == accion);
        if (desdeUtc.HasValue)             query = query.Where(m => m.FechaUtc >= desdeUtc);
        if (antesDeUtc.HasValue)           query = query.Where(m => m.FechaUtc < antesDeUtc);

        var total = await query.CountAsync();

        var movimientos = await query
            .Include(m => m.Usuario)
            .OrderByDescending(m => m.FechaUtc)
            .ThenByDescending(m => m.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (total, movimientos);
    }
}
