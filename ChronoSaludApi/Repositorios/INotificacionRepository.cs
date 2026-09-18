using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface INotificacionRepository
{
    Task<IEnumerable<Notificacion>> ObtenerDeUsuario(int usuarioId, bool? leida, string? tipo);
    Task<Notificacion?> ObtenerPorId(int id);
    Task Agregar(Notificacion notificacion);
    Task Actualizar(Notificacion notificacion);
}
