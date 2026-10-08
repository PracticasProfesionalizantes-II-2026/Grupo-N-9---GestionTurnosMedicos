using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

/// <summary>
/// Respuestas que comparten las secciones: volver al login, permiso insuficiente
/// y "no encontrado". Las vistas viven en Views/Shared y los enlaces de vuelta
/// resuelven al Index del controlador que las usa.
/// </summary>
public abstract class ControladorBase : Controller
{
    protected IActionResult AlLogin(string? destino) =>
        RedirectToAction("Login", "Cuenta", new { returnUrl = destino });

    /// <summary>
    /// Con <paramref name="volverAlInicio"/> el enlace de vuelta va al inicio
    /// y no al Index del controlador: es para cuando ese Index tampoco le
    /// corresponde al rol.
    /// </summary>
    protected IActionResult SinPermiso(string titulo, string motivo, bool volverAlInicio = false)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Titulo"] = titulo;
        ViewData["Motivo"] = motivo;
        ViewData["VolverAlInicio"] = volverAlInicio;
        return View("SinPermiso", HttpContext.Session.ObtenerSesion()?.Rol);
    }

    protected IActionResult NoEncontrado(int id, string titulo, string entidad)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Titulo"] = titulo;
        ViewData["Entidad"] = entidad;
        return View("NoEncontrado", id);
    }
}
