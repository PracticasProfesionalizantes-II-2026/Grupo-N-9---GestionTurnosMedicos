using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class TurnosController : Controller
{
    // La API pagina de a 20 por defecto. Pedimos más para que el resumen de
    // arriba cuente sobre algo representativo mientras no haya paginado propio.
    private const int Limite = 100;

    private readonly TurnoService _turnos;
    private readonly AuthService _auth;

    public TurnosController(TurnoService turnos, AuthService auth)
    {
        _turnos = turnos;
        _auth = auth;
    }

    public async Task<IActionResult> Index()
    {
        if (!_auth.HaySesion)
            return RedirectToAction("Login", "Cuenta", new { returnUrl = Url.Action(nameof(Index)) });

        try
        {
            var pagina = await _turnos.ObtenerAsync(limite: Limite);

            return View(new TurnosIndexViewModel
            {
                Total  = pagina.Total,
                Turnos = pagina.Turnos.Select(Mapear).ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // El 401 no se atrapa a propósito: lo maneja ApiExceptionFilter
            // mandando al login. El resto se muestra dentro de la página.
            return View(new TurnosIndexViewModel { Error = error.Message });
        }
    }

    private static TurnoFilaViewModel Mapear(TurnoLista turno) => new()
    {
        IdTurno     = turno.IdTurno,
        FechaInicio = turno.FechaInicio,
        Estado      = turno.Estado,
        Paciente    = turno.Paciente,
        Doctor      = turno.Doctor,
        // La API manda FechaInicio a las 00:00 cuando no hay hora que informar.
        // No inventamos una: la vista muestra un guion.
        Hora = turno.FechaInicio.TimeOfDay > TimeSpan.Zero
            ? turno.FechaInicio.ToString("HH:mm")
            : null
    };
}
