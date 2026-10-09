using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;
using Microsoft.Extensions.Configuration;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas del cambio de contraseña (POST /usuarios/me/contrasena), de que el
/// PUT de la cuenta ya no la cambia, y del tope de intentos.
/// </summary>
public class ContrasenaTests
{
    private const string Actual = "Clave2026!";

    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly UsuarioRepositoryFalso _usuarios;
    private readonly UsuarioLogica _logica;
    private readonly Usuario _usuario;

    public ContrasenaTests()
    {
        _usuarios = new UsuarioRepositoryFalso(_pacientes);

        var valores = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-prueba-de-al-menos-32-caracteres",
            ["Jwt:Issuer"] = "ChronoSaludApi",
            ["Jwt:Audience"] = "ChronoSaludClients"
        };
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        _logica = new UsuarioLogica(_usuarios, _pacientes, configuracion);

        // Un usuario con la contraseña guardada como hash, igual que en la base.
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

    [Fact]
    public async Task Con_la_actual_correcta_cambia_la_contrasena()
    {
        // Ejecutar
        var (ok, error) = await _logica.CambiarContrasena(1, new CambioContrasenaDto(Actual, "OtraClave2026"));

        // Verificar: la nueva sirve y la vieja ya no.
        Assert.True(ok);
        Assert.Null(error);
        Assert.True(BCrypt.Net.BCrypt.Verify("OtraClave2026", _usuario.Contrasena));
        Assert.False(BCrypt.Net.BCrypt.Verify(Actual, _usuario.Contrasena));
    }

    [Theory]
    [InlineData("ClaveEquivocada", "OtraClave2026", "La contraseña actual no es correcta.")]
    [InlineData(Actual, "corta", "La contraseña nueva debe tener al menos 8 caracteres.")]
    [InlineData(Actual, Actual, "La contraseña nueva tiene que ser distinta de la actual.")]
    public async Task Rechaza_y_no_cambia_nada(string actual, string nueva, string esperado)
    {
        var hashAntes = _usuario.Contrasena;

        var (ok, error) = await _logica.CambiarContrasena(1, new CambioContrasenaDto(actual, nueva));

        Assert.False(ok);
        Assert.Equal(esperado, error);
        Assert.Equal(hashAntes, _usuario.Contrasena);
    }

    [Fact]
    public async Task Un_usuario_dado_de_baja_no_cambia_la_contrasena()
    {
        _usuario.Activo = false;

        var (ok, error) = await _logica.CambiarContrasena(1, new CambioContrasenaDto(Actual, "OtraClave2026"));

        Assert.False(ok);
        Assert.Equal("Usuario no encontrado.", error);
    }

    [Fact]
    public async Task Actualizar_la_cuenta_no_toca_la_contrasena()
    {
        // Preparar: el PUT solo trae nombre, apellido y teléfono.
        var hashAntes = _usuario.Contrasena;

        // Ejecutar
        var (ok, _) = await _logica.Actualizar(1, new UsuarioUpdateDto("Ana María", null, "11-5555-0000"));

        // Verificar
        Assert.True(ok);
        Assert.Equal("Ana María", _usuario.Nombre);
        Assert.Equal(hashAntes, _usuario.Contrasena);
    }

    [Fact]
    public void El_login_corta_en_el_intento_once_y_solo_para_ese_email()
    {
        using var limite = new LimiteIntentos();

        // Los primeros 10 intentos entran (sin importar mayúsculas ni espacios).
        for (var i = 0; i < 10; i++)
        {
            Assert.True(limite.PermiteLogin(i % 2 == 0 ? "ana@demo.com" : " ANA@demo.com "));
        }

        // El 11 no, pero otro email sigue entrando.
        Assert.False(limite.PermiteLogin("ana@demo.com"));
        Assert.True(limite.PermiteLogin("otra@demo.com"));
    }

    [Fact]
    public void El_cambio_de_contrasena_corta_en_el_intento_seis()
    {
        using var limite = new LimiteIntentos();

        for (var i = 0; i < 5; i++)
        {
            Assert.True(limite.PermiteCambioDeContrasena(1));
        }

        Assert.False(limite.PermiteCambioDeContrasena(1));
        Assert.True(limite.PermiteCambioDeContrasena(2));
    }
}
