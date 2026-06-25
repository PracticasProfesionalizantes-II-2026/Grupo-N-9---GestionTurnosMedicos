using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface INotificacionLogica
{
    Task<(int total, IEnumerable<NotificacionDto> notificaciones)> ObtenerDeUsuario(int usuarioId, bool? leida, string? tipo, int pagina);
    Task<(bool ok, string? error)> MarcarLeida(int id);
}
