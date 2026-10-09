using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>Reemplaza a NotificacionRepository: guarda lo que se notificó en una lista.</summary>
public class NotificacionRepositoryFalso : INotificacionRepository
{
    public List<Notificacion> Notificaciones { get; } = new List<Notificacion>();

    public Task<(int total, List<Notificacion> notificaciones)> ObtenerDeUsuario(
        int usuarioId, bool? leida, string? tipo, int pagina, int limite)
    {
        var delUsuario = Notificaciones.Where(n => n.IdUsuario == usuarioId).ToList();
        var deLaPagina = delUsuario.Skip((pagina - 1) * limite).Take(limite).ToList();
        return Task.FromResult((delUsuario.Count, deLaPagina));
    }

    public Task<Notificacion?> ObtenerPorId(int id)
    {
        var notificacion = Notificaciones.FirstOrDefault(n => n.Id == id);
        return Task.FromResult(notificacion);
    }

    public Task<int> ContarNoLeidas(int usuarioId)
    {
        var cuantas = Notificaciones.Count(n => n.IdUsuario == usuarioId && !n.Leida);
        return Task.FromResult(cuantas);
    }

    public Task<int> MarcarTodasLeidas(int usuarioId)
    {
        var marcadas = 0;
        foreach (var notificacion in Notificaciones)
        {
            if (notificacion.IdUsuario != usuarioId || notificacion.Leida) continue;
            notificacion.Leida = true;
            marcadas++;
        }
        return Task.FromResult(marcadas);
    }

    public Task Agregar(Notificacion notificacion)
    {
        notificacion.Id = Notificaciones.Count + 1;
        Notificaciones.Add(notificacion);
        return Task.CompletedTask;
    }

    public Task Actualizar(Notificacion notificacion) => Task.CompletedTask;
}
