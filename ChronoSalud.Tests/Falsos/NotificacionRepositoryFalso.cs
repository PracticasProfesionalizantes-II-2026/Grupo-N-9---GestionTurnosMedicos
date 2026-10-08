using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a NotificacionRepository: guarda lo que se notificó en una lista.</summary>
public class NotificacionRepositoryFalso : INotificacionRepository
{
    public List<Notificacion> Notificaciones { get; } = new List<Notificacion>();

    public Task<IEnumerable<Notificacion>> ObtenerDeUsuario(int usuarioId, bool? leida, string? tipo)
    {
        var delUsuario = Notificaciones.Where(n => n.IdUsuario == usuarioId).ToList();
        return Task.FromResult<IEnumerable<Notificacion>>(delUsuario);
    }

    public Task<Notificacion?> ObtenerPorId(int id)
    {
        var notificacion = Notificaciones.FirstOrDefault(n => n.Id == id);
        return Task.FromResult(notificacion);
    }

    public Task Agregar(Notificacion notificacion)
    {
        notificacion.Id = Notificaciones.Count + 1;
        Notificaciones.Add(notificacion);
        return Task.CompletedTask;
    }

    public Task Actualizar(Notificacion notificacion) => Task.CompletedTask;
}
