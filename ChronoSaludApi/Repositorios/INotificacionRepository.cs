using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface INotificacionRepository
{
    Task<(int total, List<Notificacion> notificaciones)> ObtenerDeUsuario(int usuarioId, bool? leida, string? tipo, int pagina, int limite);
    Task<Notificacion?> ObtenerPorId(int id);
    Task Agregar(Notificacion notificacion);
    Task Actualizar(Notificacion notificacion);
    Task<int> ContarNoLeidas(int usuarioId);
    Task<int> MarcarTodasLeidas(int usuarioId);

    /// <summary>
    /// Si el usuario ya tiene una notificación de ese tipo, con ese mismo
    /// texto, desde esa fecha. El recordatorio del día lo usa para no mandarse
    /// dos veces.
    /// </summary>
    Task<bool> Existe(int usuarioId, string tipo, string mensaje, DateTime desde);
}
