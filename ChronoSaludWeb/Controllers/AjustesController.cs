using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

/// <summary>
/// Preferencias visuales de la app. A diferencia del resto de las secciones no
/// pide sesión ni habla con la API: el tema y la escala son del navegador, así
/// que tienen que funcionar también para quien todavía no inició sesión.
/// </summary>
public class AjustesController : ControladorBase
{
    public IActionResult Index()
    {
        var preferencias = Request.LeerPreferencias();

        return View(new AjustesViewModel
        {
            Tema = preferencias.Tema,
            Escala = preferencias.Escala,
            UrlRetorno = UrlDeOrigen()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Guardar(AjustesViewModel modelo)
    {
        // Los radios tienen un conjunto cerrado de valores, pero el POST se
        // puede armar a mano: las extensiones descartan cualquier cosa que no
        // esté en la lista blanca y dejan el default.
        Response.GuardarPreferencias(new PreferenciasVisuales(
            PreferenciasExtensiones.TemaValido(modelo.Tema),
            PreferenciasExtensiones.EscalaValida(modelo.Escala)));

        TempData["Exito"] = "Listo, guardamos tus preferencias de visualización.";

        return Url.IsLocalUrl(modelo.UrlRetorno)
            ? Redirect(modelo.UrlRetorno!)
            : RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Ruta de la pantalla desde la que se entró a Ajustes, sacada del Referer.
    /// Se devuelve solo el path para no redirigir nunca fuera del sitio, y solo
    /// si el referer es de este mismo host.
    /// </summary>
    private string? UrlDeOrigen()
    {
        var referer = Request.Headers.Referer.ToString();

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var origen))
            return null;

        if (!string.Equals(origen.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            return null;

        var ruta = origen.PathAndQuery;
        return Url.IsLocalUrl(ruta) ? ruta : null;
    }
}
