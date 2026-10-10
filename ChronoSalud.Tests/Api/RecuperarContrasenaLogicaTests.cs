using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using Microsoft.Extensions.Configuration;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// "Olvidé mi contraseña" (POST /usuarios/recuperar y /usuarios/restablecer):
/// el enlace llega solo a cuentas activas, vale una vez y la contraseña nueva
/// sigue las mismas reglas que en Mi perfil.
/// </summary>
public class RecuperarContrasenaLogicaTests
{
    private const string Actual = "Clave2026!";

    private readonly UsuarioRepositoryFalso _usuarios = new(new PacienteRepositoryFalso());
    private readonly EnviadorDeCorreoFalso _correo = new();
    private readonly RelojFijo _reloj = new() { Momento = new DateTime(2026, 10, 10, 10, 0, 0) };
    private readonly RecuperarContrasenaLogica _logica;
    private readonly Usuario _usuario;

    public RecuperarContrasenaLogicaTests()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-prueba-de-al-menos-32-caracteres",
            ["Recuperacion:UrlWeb"] = "https://chronosalud.ejemplo/"
        }).Build();

        _logica = new RecuperarContrasenaLogica(_usuarios, _reloj, configuracion, _correo);

        _usuario = new Usuario
        {
            Id = 1,
            Nombre = "Ana",
            Apellido = "Duarte",
            Email = "ana@demo.com",
            Rol = "paciente",
            Activo = true,
            Contrasena = BCrypt.Net.BCrypt.HashPassword(Actual)
        };
        _usuarios.Usuarios.Add(_usuario);
    }

    /// <summary>Saca el token del enlace del último email "mandado".</summary>
    private string TokenDelEmail()
    {
        var texto = _correo.Enviados.Last().Texto;
        var inicio = texto.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var fin = texto.IndexOfAny(new[] { '\n', ' ' }, inicio);
        return Uri.UnescapeDataString(texto[inicio..(fin < 0 ? texto.Length : fin)]);
    }

    [Fact]
    public async Task Con_un_email_registrado_manda_el_enlace()
    {
        await _logica.Pedir("ana@demo.com");

        var email = Assert.Single(_correo.Enviados);
        Assert.Equal("ana@demo.com", email.Para);
        Assert.Contains("https://chronosalud.ejemplo/Cuenta/Restablecer?token=", email.Texto);
    }

    [Fact]
    public async Task Con_un_email_que_no_existe_no_manda_nada()
    {
        await _logica.Pedir("nadie@demo.com");

        Assert.Empty(_correo.Enviados);
    }

    [Fact]
    public async Task A_una_cuenta_dada_de_baja_no_le_manda_nada()
    {
        _usuario.Activo = false;

        await _logica.Pedir("ana@demo.com");

        Assert.Empty(_correo.Enviados);
    }

    [Fact]
    public async Task Con_el_enlace_cambia_la_contrasena()
    {
        await _logica.Pedir("ana@demo.com");

        var error = await _logica.Restablecer(TokenDelEmail(), "NuevaClave2026");

        Assert.Null(error);
        Assert.True(BCrypt.Net.BCrypt.Verify("NuevaClave2026", _usuario.Contrasena));
        Assert.False(BCrypt.Net.BCrypt.Verify(Actual, _usuario.Contrasena));
    }

    [Fact]
    public async Task El_mismo_enlace_no_se_puede_usar_dos_veces()
    {
        await _logica.Pedir("ana@demo.com");
        var token = TokenDelEmail();
        await _logica.Restablecer(token, "NuevaClave2026");

        var error = await _logica.Restablecer(token, "OtraClave2026");

        Assert.Equal(RecuperarContrasenaLogica.TokenInvalido, error);
        Assert.True(BCrypt.Net.BCrypt.Verify("NuevaClave2026", _usuario.Contrasena));
    }

    [Fact]
    public async Task Pasada_una_hora_el_enlace_vence()
    {
        await _logica.Pedir("ana@demo.com");
        _reloj.Momento = _reloj.Momento.AddHours(2);

        var error = await _logica.Restablecer(TokenDelEmail(), "NuevaClave2026");

        Assert.Equal(RecuperarContrasenaLogica.TokenInvalido, error);
        Assert.True(BCrypt.Net.BCrypt.Verify(Actual, _usuario.Contrasena));
    }

    [Fact]
    public async Task Una_contrasena_corta_no_se_acepta()
    {
        await _logica.Pedir("ana@demo.com");

        var error = await _logica.Restablecer(TokenDelEmail(), "corta");

        Assert.Equal("La contraseña nueva debe tener al menos 8 caracteres.", error);
        Assert.True(BCrypt.Net.BCrypt.Verify(Actual, _usuario.Contrasena));
    }

    [Fact]
    public async Task Un_token_inventado_no_sirve()
    {
        var error = await _logica.Restablecer("1.999999999999999999.inventada", "NuevaClave2026");

        Assert.Equal(RecuperarContrasenaLogica.TokenInvalido, error);
    }
}
