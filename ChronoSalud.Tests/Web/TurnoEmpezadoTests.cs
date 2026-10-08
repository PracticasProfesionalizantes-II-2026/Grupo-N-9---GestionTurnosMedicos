using ChronoSaludWeb.Models;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Qué acciones ofrece la Web según si el turno ya empezó: completar y
/// marcar ausente recién a la hora del turno.
/// </summary>
public class TurnoEmpezadoTests
{
    private static readonly DateTime Hoy = new DateTime(2026, 10, 8);

    [Fact]
    public void Antes_de_la_hora_no_se_completa_ni_se_marca_ausente()
    {
        // Preparar: turno de las 10:00, son las 09:59.
        var turno = new TurnoDetalleViewModel
        {
            Estado = "confirmado",
            FechaInicio = Hoy,
            HoraInicio = "10:00",
            Ahora = Hoy.AddHours(9).AddMinutes(59)
        };

        // Verificar
        Assert.False(turno.YaEmpezo);
        Assert.False(turno.PuedeCompletarse);
        Assert.False(turno.PuedeMarcarAusente);
        Assert.True(turno.PuedeCancelarse);
    }

    [Fact]
    public void A_la_hora_del_turno_ya_se_puede_completar_o_marcar_ausente()
    {
        var turno = new TurnoDetalleViewModel
        {
            Estado = "pendiente",
            FechaInicio = Hoy,
            HoraInicio = "10:00",
            Ahora = Hoy.AddHours(10)
        };

        Assert.True(turno.YaEmpezo);
        Assert.True(turno.PuedeCompletarse);
        Assert.True(turno.PuedeMarcarAusente);
    }

    [Fact]
    public void Un_turno_ausente_es_final()
    {
        var turno = new TurnoDetalleViewModel { Estado = "ausente", FechaInicio = Hoy, HoraInicio = "10:00", Ahora = Hoy.AddHours(12) };

        Assert.True(turno.EsTerminal);
        Assert.False(turno.PuedeCancelarse);
        Assert.False(turno.PuedeMarcarAusente);
    }

    [Fact]
    public void La_fila_del_listado_sabe_si_el_turno_ya_empezo()
    {
        var empezado = new TurnoFilaViewModel { FechaInicio = Hoy, Hora = "08:30", Ahora = Hoy.AddHours(9) };
        var futuro = new TurnoFilaViewModel { FechaInicio = Hoy.AddDays(1), Hora = "08:30", Ahora = Hoy.AddHours(9) };

        Assert.True(empezado.YaEmpezo);
        Assert.False(futuro.YaEmpezo);
    }
}
