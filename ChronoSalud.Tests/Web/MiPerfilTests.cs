using System.ComponentModel.DataAnnotations;
using ChronoSalud.Tests.Falsos;
using ChronoSaludWeb.Controllers;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;
using Microsoft.AspNetCore.Http;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de Mi perfil: el formulario de la contraseña, la ficha que manda el
/// propio paciente y el menú.
/// </summary>
public class MiPerfilTests
{
    [Fact]
    public void La_contrasena_nueva_correcta_pasa_la_validacion()
    {
        var formulario = new CambioContrasenaViewModel
        {
            Actual = "Chrono2026!",
            Nueva = "OtraClave2026",
            Repetir = "OtraClave2026"
        };

        Assert.Empty(Validar(formulario));
    }

    [Fact]
    public void La_contrasena_nueva_corta_no_pasa()
    {
        var formulario = new CambioContrasenaViewModel
        {
            Actual = "Chrono2026!",
            Nueva = "corta",
            Repetir = "corta"
        };

        var errores = Validar(formulario);

        Assert.Contains(errores, error => error.MemberNames.Contains(nameof(CambioContrasenaViewModel.Nueva)));
    }

    [Fact]
    public void Si_las_dos_nuevas_no_coinciden_no_pasa()
    {
        var formulario = new CambioContrasenaViewModel
        {
            Actual = "Chrono2026!",
            Nueva = "OtraClave2026",
            Repetir = "OtraClave2027"
        };

        var errores = Validar(formulario);

        Assert.Contains(errores, error => error.MemberNames.Contains(nameof(CambioContrasenaViewModel.Repetir)));
    }

    [Theory]
    [InlineData("La contraseña actual no es correcta.", nameof(CambioContrasenaViewModel.Actual))]
    [InlineData("La contraseña nueva debe tener al menos 8 caracteres.", nameof(CambioContrasenaViewModel.Nueva))]
    [InlineData("La contraseña nueva tiene que ser distinta de la actual.", nameof(CambioContrasenaViewModel.Nueva))]
    public void El_error_de_la_API_va_debajo_del_campo_que_corresponde(string mensaje, string campo)
    {
        Assert.Equal(campo, MiPerfilController.CampoDelError(mensaje));
    }

    [Fact]
    public void El_paciente_no_pide_borrar_su_DNI()
    {
        // Preparar: el paciente dejó vacíos el DNI y las alergias.
        var formulario = new PacienteEditarViewModel { Dni = "", Alergias = "" };

        // Ejecutar
        var ficha = formulario.ArmarFicha(esPersonal: false);

        // Verificar: las alergias se borran, el DNI no (la API se lo rechazaría).
        Assert.NotNull(ficha.Borrar);
        Assert.Contains("alergias", ficha.Borrar);
        Assert.DoesNotContain("dni", ficha.Borrar);
        Assert.Null(ficha.Dni);
    }

    [Fact]
    public void La_administracion_si_puede_borrar_el_DNI()
    {
        var formulario = new PacienteEditarViewModel { Dni = "" };

        var ficha = formulario.ArmarFicha();

        Assert.NotNull(ficha.Borrar);
        Assert.Contains("dni", ficha.Borrar);
    }

    [Fact]
    public void El_formulario_de_la_ficha_se_carga_con_lo_guardado()
    {
        var guardado = new PacienteDetalle(
            7, "Ana", "Duarte", new DateTime(1990, 5, 20), "femenino", "A+", "Penicilina", null,
            Dni: "30111222", Provincia: "Córdoba");

        var formulario = PacienteEditarViewModel.Desde(guardado);

        Assert.Equal("30111222", formulario.Dni);
        Assert.Equal(new DateOnly(1990, 5, 20), formulario.FechaNacimiento);
        Assert.Equal("Córdoba", formulario.Provincia);
        Assert.Equal("Penicilina", formulario.Alergias);
        Assert.Null(formulario.Condiciones);
    }

    [Fact]
    public void En_Mi_perfil_el_menu_marca_Mi_perfil_y_Mas()
    {
        var menu = MenuViewModel.Armar(AuthConRol("paciente"), "MiPerfil", "Editar");

        Assert.True(menu.MiPerfilActivo);
        Assert.True(menu.MasActivo);
        Assert.False(menu.AjustesActivo);
    }

    [Fact]
    public void En_otra_pantalla_Mi_perfil_no_se_marca()
    {
        var menu = MenuViewModel.Armar(AuthConRol("doctor"), "Turnos", "Index");

        Assert.False(menu.MiPerfilActivo);
        Assert.False(menu.MasActivo);
    }

    /// <summary>Corre las validaciones de los atributos, como hace MVC al recibir el formulario.</summary>
    private static List<ValidationResult> Validar(object modelo)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return errores;
    }

    /// <summary>Un AuthService con una sesión abierta del rol pedido.</summary>
    private static AuthService AuthConRol(string rol)
    {
        var contexto = new DefaultHttpContext();
        contexto.Session = new SesionFalsa();
        contexto.Session.GuardarSesion(new SesionUsuario("token-de-prueba", rol, 1, "Prueba"));

        var accesor = new HttpContextAccessor { HttpContext = contexto };

        // El ApiClient no se usa en estas pruebas: solo hace falta para crear el AuthService.
        return new AuthService(new ApiClient(new HttpClient(), accesor), accesor);
    }
}
