using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class NotificacionRepository : INotificacionRepository
{
    private readonly AppDbContext _db;

    public NotificacionRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Notificacion>> ObtenerDeUsuario(int usuarioId, bool? leida, string? tipo)
    {
        var query = _db.Notificaciones.Where(n => n.IdUsuario == usuarioId).AsQueryable();
        if (leida.HasValue)                query = query.Where(n => n.Leida == leida);
        if (!string.IsNullOrEmpty(tipo))   query = query.Where(n => n.Tipo == tipo);
        return await query.OrderByDescending(n => n.Fecha).ToListAsync();
    }

    public async Task<Notificacion?> ObtenerPorId(int id)
        => await _db.Notificaciones.FindAsync(id);

    public async Task Actualizar(Notificacion notificacion)
    {
        _db.Notificaciones.Update(notificacion);
        await _db.SaveChangesAsync();
    }
}
