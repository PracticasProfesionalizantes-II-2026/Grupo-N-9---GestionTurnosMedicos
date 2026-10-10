using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// "Hoy" tiene que ser el día de Argentina aunque el servidor esté en UTC, y
/// la fecha corta se escribe igual en Windows y en Linux.
/// </summary>
public class FechaArgentinaTests
{
    [Fact]
    public void A_las_23_30_de_Argentina_todavia_es_el_mismo_dia()
    {
        // Las 02:30 UTC del 9 de octubre son las 23:30 del 8 en Argentina.
        var instanteUtc = new DateTime(2026, 10, 9, 2, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 10, 8), FechaArgentina.Hoy(instanteUtc));
    }

    [Fact]
    public void Al_mediodia_es_el_mismo_dia_en_los_dos_relojes()
    {
        var instanteUtc = new DateTime(2026, 10, 9, 15, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 10, 9), FechaArgentina.Hoy(instanteUtc));
    }

    [Fact]
    public void La_fecha_corta_va_sin_punto_en_el_mes()
    {
        // Windows escribe "oct." y Linux "oct": tiene que salir igual en los dos.
        Assert.Equal("9 oct 2026", FechaArgentina.Corta(new DateTime(2026, 10, 9)));
        Assert.Equal("9 oct 2026, 10:30", FechaArgentina.CortaConHora(new DateTime(2026, 10, 9, 10, 30, 0)));
    }

    [Fact]
    public void Ningun_mes_queda_con_punto()
    {
        for (var mes = 1; mes <= 12; mes++)
        {
            var texto = FechaArgentina.Corta(new DateTime(2026, mes, 15));

            Assert.DoesNotContain(".", texto);
            Assert.StartsWith("15 ", texto);
            Assert.EndsWith(" 2026", texto);
        }
    }
}
