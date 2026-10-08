using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Api;

/// <summary>Pruebas de la hora de Argentina que usa la API.</summary>
public class FechaArgentinaApiTests
{
    [Fact]
    public void Un_dia_de_Argentina_empieza_a_las_3_de_la_manana_en_UTC()
    {
        // Argentina está en UTC-3: su medianoche son las 03:00 en UTC.
        var inicio = FechaArgentina.ComienzoDelDiaEnUtc(new DateTime(2026, 10, 8));

        Assert.Equal(new DateTime(2026, 10, 8, 3, 0, 0), inicio);
    }
}
