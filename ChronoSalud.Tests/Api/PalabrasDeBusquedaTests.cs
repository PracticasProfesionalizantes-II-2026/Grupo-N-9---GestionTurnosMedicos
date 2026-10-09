using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de cómo se separa el texto de GET /pacientes?buscar= (paso 12).
/// </summary>
public class PalabrasDeBusquedaTests
{
    [Fact]
    public void Separa_por_espacios_e_ignora_los_de_mas()
    {
        var palabras = PalabrasDeBusqueda.Separar("  ana   duarte\t30111 ");

        Assert.Equal(new[] { "ana", "duarte", "30111" }, palabras);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_texto_no_hay_palabras(string? texto)
    {
        Assert.Empty(PalabrasDeBusqueda.Separar(texto));
    }

    [Fact]
    public void Usa_como_mucho_cinco_palabras()
    {
        var palabras = PalabrasDeBusqueda.Separar("uno dos tres cuatro cinco seis siete");

        Assert.Equal(PalabrasDeBusqueda.Maximo, palabras.Count);
        Assert.Equal("cinco", palabras[^1]);
    }
}
