using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// "Hoy" tiene que ser el día de Argentina aunque el servidor esté en UTC.
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
}
