using ChronoSalud.Tests.Falsos;
using ChronoSaludWeb.Services;
using Microsoft.AspNetCore.Http;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Qué puede hacer cada rol con los turnos desde la Web. Que el turno sea del
/// usuario lo revisan después el controlador y la API; acá solo se mira el rol.
/// </summary>
public class PermisosTurnosTests
{
    // [Theory] corre la prueba una vez por cada [InlineData]: el rol y lo que
    // se espera para cancelar y para cambiar el estado.
    [Theory]
    [InlineData("paciente", true, false)]
    [InlineData("doctor", true, true)]
    [InlineData("administrador", true, true)]
    public void Cada_rol_ve_las_acciones_que_le_corresponden(string rol, bool cancela, bool cambiaEstado)
    {
        var auth = AuthConRol(rol);

        Assert.Equal(cancela, auth.PuedeCancelarTurnos);
        Assert.Equal(cambiaEstado, auth.PuedeCambiarEstadoTurno);
    }

    [Fact]
    public void Sin_sesion_no_se_puede_cancelar()
    {
        var contexto = new DefaultHttpContext();
        contexto.Session = new SesionFalsa();
        var accesor = new HttpContextAccessor { HttpContext = contexto };
        var auth = new AuthService(new ApiClient(new HttpClient(), accesor), accesor);

        Assert.False(auth.PuedeCancelarTurnos);
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
