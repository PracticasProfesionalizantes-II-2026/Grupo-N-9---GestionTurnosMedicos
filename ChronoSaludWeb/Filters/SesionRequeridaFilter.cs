using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Filters;

/// <summary>
/// Marca un controlador o una acción que se puede usar sin haber iniciado
/// sesión (portada, login, registro, ajustes). Todo lo que no lo lleve queda
/// detrás de <see cref="SesionRequeridaFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermiteSinSesionAttribute : Attribute
{
}

/// <summary>
/// Sin sesión no se entra a ninguna acción, salvo las marcadas con
/// <see cref="PermiteSinSesionAttribute"/>. Es la red de seguridad de los
/// chequeos que cada acción ya hace con AuthService.HaySesion: una acción
/// nueva que se olvide del suyo queda igual detrás del login. El rol lo sigue
/// revisando cada acción.
/// </summary>
public class SesionRequeridaFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext contexto)
    {
        if (contexto.ActionDescriptor.EndpointMetadata.OfType<PermiteSinSesionAttribute>().Any())
            return;

        if (contexto.HttpContext.Session.ObtenerSesion() is not null)
            return;

        // Igual que ApiExceptionFilter: al login, recordando a dónde iba. Un
        // POST no se puede repetir después de entrar, así que ahí se vuelve a
        // la pantalla desde la que se envió el formulario.
        var pedido = contexto.HttpContext.Request;
        var destino = HttpMethods.IsGet(pedido.Method)
            ? pedido.Path + pedido.QueryString
            : RutaDeOrigen(pedido);

        contexto.Result = new RedirectToActionResult("Login", "Cuenta", new { returnUrl = destino });
    }

    public void OnActionExecuted(ActionExecutedContext contexto)
    {
    }

    /// <summary>
    /// Ruta de la pantalla anterior, sacada del Referer, solo si es de este
    /// mismo sitio. No hace falta más: el login la vuelve a validar con
    /// Url.IsLocalUrl antes de seguirla.
    /// </summary>
    private static string? RutaDeOrigen(HttpRequest pedido)
    {
        var referer = pedido.Headers.Referer.ToString();

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var origen))
            return null;

        return string.Equals(origen.Host, pedido.Host.Host, StringComparison.OrdinalIgnoreCase)
            ? origen.PathAndQuery
            : null;
    }
}
