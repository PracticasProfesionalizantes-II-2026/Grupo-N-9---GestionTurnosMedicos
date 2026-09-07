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

    protected IActionResult SinPermiso(string titulo, string motivo)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Titulo"] = titulo;
        ViewData["Motivo"] = motivo;
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
