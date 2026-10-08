using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class NotificacionLogica : INotificacionLogica
{
    private readonly INotificacionRepository _repo;

    public NotificacionLogica(INotificacionRepository repo) => _repo = repo;

    public async Task<(int total, IEnumerable<NotificacionDto> notificaciones)> ObtenerDeUsuario(
        int usuarioId, bool? leida, string? tipo, int pagina)
    {
        // De a 20 por página, como siempre.
        var (total, notificaciones) = await _repo.ObtenerDeUsuario(usuarioId, leida, tipo, Math.Max(pagina, 1), 20);
        var resultado = notificaciones
            .Select(n => new NotificacionDto(n.Id, n.Tipo, n.Mensaje, n.Fecha, n.Leida));
        return (total, resultado);
    }

    public async Task<(bool ok, string? error, bool prohibido)> MarcarLeida(int id, int idUsuarioCaller)
    {
        var notif = await _repo.ObtenerPorId(id);
        if (notif == null) return (false, "Notificación no encontrada.", false);

        // Marcar como leída es un acto del destinatario: acá no hay excepción
        // para el staff, que sí puede listar las notificaciones de otro.
        if (notif.IdUsuario != idUsuarioCaller)
            return (false, null, true);

        notif.Leida = true;
        await _repo.Actualizar(notif);
        return (true, null, false);
    }
}
