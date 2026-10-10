using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Las fechas que lee el paciente van como se dicen: "hoy", "mañana" o
/// "viernes 16 de octubre", y no "16 oct 2026".
/// </summary>
public class FechaNaturalTests
{
    // Sábado 10 de octubre de 2026.
    private static readonly DateTime Hoy = new(2026, 10, 10);

    [Fact]
    public void El_mismo_dia_es_hoy()
    {
        Assert.Equal("hoy", FechaArgentina.DiaNatural(new DateTime(2026, 10, 10), Hoy));
    }

    [Fact]
    public void El_dia_siguiente_es_manana()
    {
        Assert.Equal("mañana", FechaArgentina.DiaNatural(new DateTime(2026, 10, 11), Hoy));
    }

    [Fact]
    public void Otro_dia_lleva_el_nombre_del_dia_y_del_mes()
    {
        Assert.Equal("viernes 16 de octubre", FechaArgentina.DiaNatural(new DateTime(2026, 10, 16), Hoy));
    }

    [Fact]
    public void Otro_anio_suma_el_anio()
    {
        Assert.Equal("viernes 15 de enero de 2027", FechaArgentina.DiaNatural(new DateTime(2027, 1, 15), Hoy));
    }

    [Fact]
    public void La_hora_del_dia_no_cambia_que_sea_hoy()
    {
        // "Ahora" trae hora: a las 18:30 un turno de hoy sigue siendo "hoy".
        Assert.Equal("hoy", FechaArgentina.DiaNatural(new DateTime(2026, 10, 10), Hoy.AddHours(18.5)));
    }

    [Fact]
    public void Con_hora_suma_hs()
    {
        Assert.Equal("viernes 16 de octubre, 14:00 hs",
            FechaArgentina.DiaNaturalConHora(new DateTime(2026, 10, 16), "14:00", Hoy));
        Assert.Equal("mañana, 9:30 hs",
            FechaArgentina.DiaNaturalConHora(new DateTime(2026, 10, 11), "9:30", Hoy));
    }

    [Fact]
    public void Sin_hora_queda_solo_el_dia()
    {
        Assert.Equal("hoy", FechaArgentina.DiaNaturalConHora(new DateTime(2026, 10, 10), null, Hoy));
        Assert.Equal("hoy", FechaArgentina.DiaNaturalConHora(new DateTime(2026, 10, 10), " ", Hoy));
    }
}
