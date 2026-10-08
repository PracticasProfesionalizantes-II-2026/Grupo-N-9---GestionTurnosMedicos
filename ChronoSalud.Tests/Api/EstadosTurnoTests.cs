using ChronoSaludApi.Logica;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Api;

/// <summary>Pruebas de los cambios de estado permitidos (EstadosTurno).</summary>
public class EstadosTurnoTests
{
    [Theory]
    // Permitidos
    [InlineData("pendiente",  "confirmado", false, true)]
    [InlineData("pendiente",  "cancelado",  false, true)]
    [InlineData("confirmado", "cancelado",  true,  true)]
    [InlineData("confirmado", "completado", true,  true)]
    [InlineData("pendiente",  "ausente",    true,  true)]
    [InlineData("completado", "completado", true,  true)]
    // No permitidos
    [InlineData("confirmado", "completado", false, false)]
    [InlineData("pendiente",  "ausente",    false, false)]
    [InlineData("confirmado", "pendiente",  true,  false)]
    [InlineData("completado", "cancelado",  true,  false)]
    [InlineData("ausente",    "confirmado", true,  false)]
    [InlineData("cancelado",  "confirmado", false, false)]
    [InlineData("pendiente",  "borrado",    true,  false)]
    public void Solo_se_permiten_los_cambios_de_la_tabla(string actual, string nuevo, bool yaEmpezo, bool esperado)
    {
        var (ok, error) = EstadosTurno.PuedeCambiar(actual, nuevo, yaEmpezo);

        Assert.Equal(esperado, ok);
        // Si no se puede, siempre se dice por qué.
        Assert.Equal(esperado, error is null);
    }

    [Fact]
    public void Los_conteos_del_listado_usan_los_mismos_estados()
    {
        // Si se agrega un estado en EstadosTurno, también tiene que contarse.
        Assert.Equal(
            EstadosTurno.Todos.OrderBy(e => e),
            FiltroTurnos.EstadosConocidos.OrderBy(e => e));
    }
}
