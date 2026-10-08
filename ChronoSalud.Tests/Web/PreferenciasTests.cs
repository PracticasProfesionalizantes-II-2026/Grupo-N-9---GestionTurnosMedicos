using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// La cookie de preferencias la manda el navegador, así que puede traer
/// cualquier cosa: solo pasan los valores de la lista blanca.
/// </summary>
public class PreferenciasTests
{
    // [Theory] corre la misma prueba una vez por cada [InlineData]:
    // el primer valor es lo que llega y el segundo, lo que se espera.
    [Theory]
    [InlineData("light", "light")]
    [InlineData("dark", "dark")]
    [InlineData("azul", "light")]
    [InlineData("\"><script>", "light")]
    [InlineData(null, "light")]
    public void Un_tema_desconocido_vuelve_al_tema_claro(string? valor, string esperado)
    {
        Assert.Equal(esperado, PreferenciasExtensiones.TemaValido(valor));
    }

    [Theory]
    [InlineData("md", "md")]
    [InlineData("lg", "lg")]
    [InlineData("xl", "xl")]
    [InlineData("xxl", "md")]
    [InlineData(null, "md")]
    public void Una_escala_desconocida_vuelve_a_la_normal(string? valor, string esperado)
    {
        Assert.Equal(esperado, PreferenciasExtensiones.EscalaValida(valor));
    }
}
