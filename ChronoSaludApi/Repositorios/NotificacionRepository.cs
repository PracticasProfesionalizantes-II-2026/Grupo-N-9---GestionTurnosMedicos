using Microsoft.EntityFrameworkCore;
using ChronoSaludApi.Datos;
using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public class NotificacionRepository : INotificacionRepository
{
    private readonly AppDbContext _db;

    public NotificacionRepository(AppDbContext db) => _db = db;

    // Una página de las notificaciones del usuario, de la más nueva a la más
    // vieja. El total y la página los resuelve SQL Server.
    public async Task<(int total, List<Notificacion> notificaciones)> ObtenerDeUsuario(
        int usuarioId, bool? leida, string? tipo, int pagina, int limite)
    {
        var query = _db.Notificaciones.AsNoTracking().Where(n => n.IdUsuario == usuarioId);
        if (leida.HasValue)                query = query.Where(n => n.Leida == leida);
        if (!string.IsNullOrEmpty(tipo))   query = query.Where(n => n.Tipo == tipo);

        var total = await query.CountAsync();

        var notificaciones = await query
            .OrderByDescending(n => n.Fecha)
            .ThenByDescending(n => n.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (total, notificaciones);
    }

    public async Task<Notificacion?> ObtenerPorId(int id)
        => await _db.Notificaciones.FindAsync(id);

    public async Task Agregar(Notificacion notificacion)
    {
        _db.Notificaciones.Add(notificacion);
        await _db.SaveChangesAsync();
    }

    public async Task Actualizar(Notificacion notificacion)
    {
        _db.Notificaciones.Update(notificacion);
        await _db.SaveChangesAsync();
    }
}
