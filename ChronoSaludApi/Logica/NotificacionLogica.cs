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
        var todas = await _repo.ObtenerDeUsuario(usuarioId, leida, tipo);
        var total = todas.Count();
        var resultado = todas
            .Skip((pagina - 1) * 20)
            .Take(20)
            .Select(n => new NotificacionDto(n.Id, n.Tipo, n.Mensaje, n.Fecha, n.Leida));
        return (total, resultado);
    }

    public async Task<(bool ok, string? error)> MarcarLeida(int id)
    {
        var notif = await _repo.ObtenerPorId(id);
        if (notif == null) return (false, "Notificación no encontrada.");

        notif.Leida = true;
        await _repo.Actualizar(notif);
        return (true, null);
    }
}
