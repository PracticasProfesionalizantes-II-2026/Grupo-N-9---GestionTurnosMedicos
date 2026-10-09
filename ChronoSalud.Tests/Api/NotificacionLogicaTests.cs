using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de contar las no leídas y de marcar todas como leídas (paso 13).
/// </summary>
public class NotificacionLogicaTests
{
    // Usuario 1: dos sin leer y una leída. Usuario 2: una sin leer.
    private static NotificacionRepositoryFalso RepositorioConNotificaciones()
    {
        var repo = new NotificacionRepositoryFalso();
        repo.Notificaciones.Add(new Notificacion { Id = 1, IdUsuario = 1, Tipo = "turno", Mensaje = "a" });
        repo.Notificaciones.Add(new Notificacion { Id = 2, IdUsuario = 1, Tipo = "turno", Mensaje = "b" });
        repo.Notificaciones.Add(new Notificacion { Id = 3, IdUsuario = 1, Tipo = "estudio", Mensaje = "c", Leida = true });
        repo.Notificaciones.Add(new Notificacion { Id = 4, IdUsuario = 2, Tipo = "turno", Mensaje = "d" });
        return repo;
    }

    [Fact]
    public async Task Cuenta_solo_las_sin_leer_del_usuario()
    {
        var logica = new NotificacionLogica(RepositorioConNotificaciones());

        Assert.Equal(2, await logica.ContarNoLeidas(1));
        Assert.Equal(1, await logica.ContarNoLeidas(2));
        Assert.Equal(0, await logica.ContarNoLeidas(99));
    }

    [Fact]
    public async Task Marcar_todas_marca_las_propias_y_devuelve_cuantas()
    {
        var repo = RepositorioConNotificaciones();
        var logica = new NotificacionLogica(repo);

        var marcadas = await logica.MarcarTodasLeidas(1);

        // La que ya estaba leída no cuenta.
        Assert.Equal(2, marcadas);
        Assert.Equal(0, await logica.ContarNoLeidas(1));
    }

    [Fact]
    public async Task Marcar_todas_no_toca_las_de_otro_usuario()
    {
        var repo = RepositorioConNotificaciones();
        var logica = new NotificacionLogica(repo);

        await logica.MarcarTodasLeidas(1);

        var deOtro = repo.Notificaciones.Single(n => n.Id == 4);
        Assert.False(deOtro.Leida);
        Assert.Equal(1, await logica.ContarNoLeidas(2));
    }

    [Fact]
    public async Task Sin_nada_para_marcar_devuelve_cero()
    {
        var logica = new NotificacionLogica(RepositorioConNotificaciones());

        await logica.MarcarTodasLeidas(1);

        Assert.Equal(0, await logica.MarcarTodasLeidas(1));
    }
}
