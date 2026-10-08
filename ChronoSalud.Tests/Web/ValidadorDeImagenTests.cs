using System.Text;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// El tipo de una foto se reconoce por sus primeros bytes, no por la extensión
/// del archivo. Estas pruebas arman esos bytes a mano.
/// </summary>
public class ValidadorDeImagenTests
{
    [Fact]
    public void Reconoce_un_jpeg()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        Assert.Equal("image/jpeg", ValidadorDeImagen.DetectarTipo(bytes));
    }

    [Fact]
    public void Reconoce_un_png()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        Assert.Equal("image/png", ValidadorDeImagen.DetectarTipo(bytes));
    }

    [Fact]
    public void Reconoce_un_webp()
    {
        // "RIFF", cuatro bytes de tamaño (acá da igual cuáles) y "WEBP".
        var bytes = Encoding.ASCII.GetBytes("RIFF0000WEBP");

        Assert.Equal("image/webp", ValidadorDeImagen.DetectarTipo(bytes));
    }

    [Fact]
    public void Rechaza_un_gif()
    {
        var bytes = Encoding.ASCII.GetBytes("GIF89a");

        Assert.Null(ValidadorDeImagen.DetectarTipo(bytes));
    }

    [Fact]
    public void Rechaza_un_archivo_vacio()
    {
        Assert.Null(ValidadorDeImagen.DetectarTipo(new byte[0]));
    }
}
