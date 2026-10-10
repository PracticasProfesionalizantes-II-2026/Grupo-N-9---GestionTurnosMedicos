using ChronoSaludWeb.Models;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Textos que lee el lector de pantalla: cada enlace de turno dice de qué
/// turno es, y el consultorio no se repite.
/// </summary>
public class TextosParaLectorTests
{
    // Sábado 10 de octubre de 2026 a las 10:00.
    private static readonly DateTime Ahora = new(2026, 10, 10, 10, 0, 0);

    private static TurnoFilaViewModel Fila(DateTime fecha, string? hora) => new()
    {
        IdTurno = 1,
        FechaInicio = fecha,
        Hora = hora,
        Estado = "confirmado",
        Ahora = Ahora
    };

    [Fact]
    public void Un_turno_de_otro_dia_dice_el_dia_y_la_hora()
    {
        Assert.Equal("del lunes 12 de octubre a las 08:00", Fila(new DateTime(2026, 10, 12), "08:00").CuandoParaLector);
    }

    [Fact]
    public void Hoy_y_manana_van_sin_del()
    {
        Assert.Equal("de hoy a las 15:00", Fila(new DateTime(2026, 10, 10), "15:00").CuandoParaLector);
        Assert.Equal("de mañana a las 09:30", Fila(new DateTime(2026, 10, 11), "09:30").CuandoParaLector);
    }

    [Fact]
    public void Sin_hora_queda_solo_el_dia()
    {
        Assert.Equal("del lunes 12 de octubre", Fila(new DateTime(2026, 10, 12), null).CuandoParaLector);
    }

    [Theory]
    [InlineData("101", "Consultorio 101")]
    [InlineData("Consultorio 101", "Consultorio 101")]
    [InlineData("consultorio 3", "consultorio 3")]
    [InlineData("  202 ", "Consultorio 202")]
    public void El_consultorio_no_repite_la_palabra(string dato, string esperado)
    {
        Assert.Equal(esperado, ConsultorioTexto.Para(dato));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_consultorio_no_hay_texto(string? dato)
    {
        Assert.Null(ConsultorioTexto.Para(dato));
    }
}
