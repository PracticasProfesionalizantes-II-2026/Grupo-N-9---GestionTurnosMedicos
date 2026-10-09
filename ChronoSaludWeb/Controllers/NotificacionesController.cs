using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

/// <summary>
/// Las notificaciones del paciente: la lista, marcar una como leída y marcar
/// todas. Abrir la pantalla no marca nada solo: cada una se marca a mano.
/// </summary>
public class NotificacionesController : ControladorBase
{
    private const string TituloSinPermiso = "Las notificaciones son para pacientes";
    private const string MotivoSinPermiso =
        "Acá llegan los avisos de los turnos y de los estudios de cada paciente.";

    // El valor de ?ver= de la pestaña "Sin leer".
    private const string VerSinLeer = "sin-leer";

    private readonly NotificacionService _notificaciones;
    private readonly AuthService _auth;

    public NotificacionesController(NotificacionService notificaciones, AuthService auth)
    {
        _notificaciones = notificaciones;
        _auth = auth;
    }

    /// <summary>Con ver=sin-leer muestra solo las sin leer.</summary>
    public async Task<IActionResult> Index(string? ver, int pagina = 1)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.VeNotificaciones)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso, volverAlInicio: true);

        var soloNoLeidas = ver == VerSinLeer;
        pagina = Math.Max(pagina, 1);

        try
        {
            var resultado = await _notificaciones.ObtenerAsync(pagina, soloNoLeidas);

            // Una página que quedó vacía (por ejemplo, al marcar la última de
            // "Sin leer") vuelve a la última que existe.
            if (resultado.Notificaciones.Count == 0 && pagina > 1)
            {
                var ultima = PaginadorViewModel.ContarPaginas(resultado.Total, NotificacionService.PorPagina);
                return RedirectToAction(nameof(Index), new { ver = soloNoLeidas ? VerSinLeer : null, pagina = ultima });
            }

            // De paso actualiza el número de la campana.
            var noLeidas = await _notificaciones.ContarNoLeidasAsync();

            var filas = new List<NotificacionFilaViewModel>();
            foreach (var notificacion in resultado.Notificaciones)
                filas.Add(NotificacionFilaViewModel.Desde(notificacion));

            return View(new NotificacionesIndexViewModel
            {
                Notificaciones = filas,
                Total = resultado.Total,
                NoLeidas = noLeidas,
                Pagina = pagina,
                SoloNoLeidas = soloNoLeidas
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new NotificacionesIndexViewModel { SoloNoLeidas = soloNoLeidas, Error = error.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(int id, string? ver, int pagina = 1)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.VeNotificaciones)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso, volverAlInicio: true);

        try
        {
            await _notificaciones.MarcarLeidaAsync(id);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = $"No se pudo marcar la notificación: {error.Message}";
        }

        // Vuelve a la misma pestaña y página.
        return RedirectToAction(nameof(Index), new { ver = ver == VerSinLeer ? VerSinLeer : null, pagina = pagina > 1 ? pagina : (int?)null });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarTodas(string? ver)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.VeNotificaciones)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso, volverAlInicio: true);

        try
        {
            var marcadas = await _notificaciones.MarcarTodasAsync();
            TempData["Exito"] = marcadas switch
            {
                0 => "No había notificaciones sin leer.",
                1 => "Marcaste 1 notificación como leída.",
                _ => $"Marcaste {marcadas} notificaciones como leídas."
            };
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = $"No se pudieron marcar las notificaciones: {error.Message}";
        }

        return RedirectToAction(nameof(Index), new { ver = ver == VerSinLeer ? VerSinLeer : null });
    }
}
