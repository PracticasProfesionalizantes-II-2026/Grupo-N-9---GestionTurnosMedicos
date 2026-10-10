using ChronoSalud.Tests.Falsos;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de la campana y de la pantalla de notificaciones (paso 13).
/// </summary>
public class NotificacionesWebTests
{
    private static readonly DateTime Ahora = new DateTime(2026, 10, 9, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void El_contador_guardado_se_lee_antes_del_minuto()
    {
        var sesion = new SesionFalsa();

        ContadorDeNoLeidas.Guardar(sesion, 3, Ahora);

        Assert.Equal(3, ContadorDeNoLeidas.Leer(sesion, Ahora.AddSeconds(59)));
    }

    [Fact]
    public void Pasado_el_minuto_el_contador_ya_no_vale()
    {
        var sesion = new SesionFalsa();

        ContadorDeNoLeidas.Guardar(sesion, 3, Ahora);

        Assert.Null(ContadorDeNoLeidas.Leer(sesion, Ahora.AddSeconds(60)));
    }

    [Fact]
    public void Sin_contador_guardado_o_borrado_no_hay_numero()
    {
        var sesion = new SesionFalsa();
        Assert.Null(ContadorDeNoLeidas.Leer(sesion, Ahora));

        ContadorDeNoLeidas.Guardar(sesion, 3, Ahora);
        ContadorDeNoLeidas.Borrar(sesion);

        Assert.Null(ContadorDeNoLeidas.Leer(sesion, Ahora));
    }

    [Theory]
    [InlineData(null, null, "Notificaciones")]
    [InlineData(0, null, "Notificaciones")]
    [InlineData(1, "1", "Notificaciones: 1 sin leer")]
    [InlineData(9, "9", "Notificaciones: 9 sin leer")]
    [InlineData(12, "9+", "Notificaciones: 12 sin leer")]
    public void La_campana_muestra_el_globito_y_el_texto_completo(int? noLeidas, string? globito, string texto)
    {
        var campana = new CampanaViewModel { NoLeidas = noLeidas };

        Assert.Equal(globito, campana.Globito);
        Assert.Equal(texto, campana.TextoAccesible);
    }

    [Theory]
    [InlineData("turno", "calendario", true)]
    [InlineData("estudio", "estudio", false)]
    [InlineData("receta", "pastilla", false)]
    [InlineData("general", "info", false)]
    public void Cada_tipo_tiene_su_icono_y_solo_los_turnos_llevan_enlace(string tipo, string icono, bool esDeTurno)
    {
        var fila = NotificacionFilaViewModel.Desde(
            new NotificacionLista(1, tipo, "Mensaje", new DateTime(2026, 10, 9, 10, 30, 0), false));

        Assert.Equal(icono, fila.Icono);
        Assert.Equal(esDeTurno, fila.EsDeTurno);
    }

    [Fact]
    public void La_fecha_se_muestra_en_castellano()
    {
        var fila = NotificacionFilaViewModel.Desde(
            new NotificacionLista(1, "turno", "Mensaje", new DateTime(2026, 10, 9, 10, 30, 0), false));

        Assert.Equal("9 oct 2026, 10:30", fila.FechaTexto);
    }

    [Fact]
    public void El_paginador_conserva_la_pestana()
    {
        var sinLeer = new NotificacionesIndexViewModel { SoloNoLeidas = true, Pagina = 2, Total = 45 };

        Assert.Equal(3, sinLeer.Paginador.TotalPaginas);
        Assert.Equal(new Dictionary<string, string> { ["ver"] = "sin-leer" }, sinLeer.Paginador.RutaAnterior);
        Assert.Equal(new Dictionary<string, string> { ["ver"] = "sin-leer", ["pagina"] = "3" }, sinLeer.Paginador.RutaSiguiente);

        // "Todas" no lleva nada en la URL.
        Assert.Empty(NotificacionesIndexViewModel.RutaPestana(false));
    }
}
