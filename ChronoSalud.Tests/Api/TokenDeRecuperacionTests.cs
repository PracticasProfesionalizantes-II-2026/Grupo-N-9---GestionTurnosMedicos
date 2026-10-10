using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// El token del enlace de "Olvidé mi contraseña": vale una hora, solo para ese
/// usuario y mientras no cambie la contraseña. Nadie puede inventarse uno.
/// </summary>
public class TokenDeRecuperacionTests
{
    private const string Clave = "clave-de-prueba-de-al-menos-32-caracteres";
    private const string Hash = "$2a$11$hashDePruebaDeLaContrasenaActual";
    private static readonly DateTime Ahora = new(2026, 10, 10, 10, 0, 0);

    [Fact]
    public void Un_token_recien_creado_es_valido_y_dice_de_quien_es()
    {
        var token = TokenDeRecuperacion.Crear(7, Hash, Ahora, Clave);

        Assert.Equal(7, TokenDeRecuperacion.LeerIdUsuario(token));
        Assert.True(TokenDeRecuperacion.EsValido(token, 7, Hash, Ahora.AddMinutes(59), Clave));
    }

    [Fact]
    public void Despues_de_una_hora_vence()
    {
        var token = TokenDeRecuperacion.Crear(7, Hash, Ahora, Clave);

        Assert.False(TokenDeRecuperacion.EsValido(token, 7, Hash, Ahora.AddMinutes(61), Clave));
    }

    [Fact]
    public void Si_la_contrasena_cambio_el_mismo_token_ya_no_sirve()
    {
        var token = TokenDeRecuperacion.Crear(7, Hash, Ahora, Clave);

        Assert.False(TokenDeRecuperacion.EsValido(token, 7, "$2a$11$otroHashDespuesDelCambio", Ahora, Clave));
    }

    [Fact]
    public void No_sirve_para_otro_usuario()
    {
        var token = TokenDeRecuperacion.Crear(7, Hash, Ahora, Clave);

        Assert.False(TokenDeRecuperacion.EsValido(token, 8, Hash, Ahora, Clave));
    }

    [Fact]
    public void Firmado_con_otra_clave_no_sirve()
    {
        var token = TokenDeRecuperacion.Crear(7, Hash, Ahora, "otra-clave-que-no-es-la-del-servidor");

        Assert.False(TokenDeRecuperacion.EsValido(token, 7, Hash, Ahora, Clave));
    }

    [Fact]
    public void Si_le_cambian_el_vencimiento_la_firma_no_coincide()
    {
        var partes = TokenDeRecuperacion.Crear(7, Hash, Ahora, Clave).Split('.');

        // Alguien intenta estirarle el vencimiento un año.
        var estirado = $"{partes[0]}.{Ahora.AddYears(1).Ticks}.{partes[2]}";

        Assert.False(TokenDeRecuperacion.EsValido(estirado, 7, Hash, Ahora, Clave));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("basura")]
    [InlineData("7.no-es-un-numero.firma")]
    [InlineData("x.1.firma")]
    [InlineData("7.99999999999999999999.firma")]
    public void Un_token_mal_formado_no_rompe_y_no_sirve(string? token)
    {
        Assert.False(TokenDeRecuperacion.EsValido(token, 7, Hash, Ahora, Clave));
    }

    [Fact]
    public void El_token_se_puede_poner_en_una_URL_sin_escapar()
    {
        var token = TokenDeRecuperacion.Crear(7, Hash, Ahora, Clave);

        Assert.Equal(token, Uri.EscapeDataString(token));
    }
}
