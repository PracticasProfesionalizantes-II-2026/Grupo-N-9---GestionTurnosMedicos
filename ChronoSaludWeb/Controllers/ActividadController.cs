using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

/// <summary>
/// El historial de movimientos (GET /movimientos): "Actividad" para el
/// administrador y "Mi actividad" para el doctor. Solo lectura.
/// </summary>
public class ActividadController : ControladorBase
{
    // Tope de la página que se le pide a la API. Más allá no hay historial
    // que mostrar, y evita mandarle un número que desborde su cuenta de filas.
    private const int PaginaMaxima = 100_000;

    private const string TituloSinPermiso = "No podés ver la actividad con tu rol";
    private const string MotivoSinPermiso =
        "El historial de toda la clínica solo lo puede ver el administrador.";

    private const string TituloSinPermisoPropia = "No tenés una actividad propia para ver";
    private const string MotivoSinPermisoPropia =
        "\"Mi actividad\" muestra lo que pasó con la agenda y el horario de un doctor, así que es solo para doctores.";

    private readonly MovimientoService _movimientos;
    private readonly DoctorService _doctores;
    private readonly AuthService _auth;

    public ActividadController(MovimientoService movimientos, DoctorService doctores, AuthService auth)
    {
        _movimientos = movimientos;
        _doctores = doctores;
        _auth = auth;
    }

    /// <summary>"Actividad": todo el historial, con quién hizo cada cambio. Solo administrador.</summary>
    public async Task<IActionResult> Index(int? doctor, string? accion, DateTime? desde, DateTime? hasta, int pagina = 1)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso, volverAlInicio: true);

        // Lo que llega por la URL se lleva a valores válidos en vez de
        // contestar con un error: una acción desconocida es "todas".
        var filtroAccion = ActividadViewModel.EsAccionConocida(accion) ? accion : null;
        pagina = Math.Clamp(pagina, 1, PaginaMaxima);

        try
        {
            var doctores = await _doctores.ObtenerTodosAsync();
            var nombres = doctores.ToDictionary(d => d.IdDoctor, d => d.Nombre);

            // Lo mismo con el doctor: uno que no está en el select es "todos".
            var filtroDoctor = doctor is { } id && nombres.ContainsKey(id) ? doctor : null;

            var resultado = await _movimientos.BuscarAsync(
                filtroDoctor, filtroAccion, desde, hasta, pagina, ActividadViewModel.PorPagina);

            if (PaginaFueraDeRango(resultado) is { } ultima)
            {
                return RedirectToAction(nameof(Index),
                    new { doctor = filtroDoctor, accion = filtroAccion, desde = Dia(desde), hasta = Dia(hasta), pagina = EnRuta(ultima) });
            }

            return View("Index", new ActividadViewModel
            {
                Doctor = filtroDoctor,
                Accion = filtroAccion,
                Desde = desde,
                Hasta = hasta,
                Pagina = pagina,
                Total = resultado.Total,
                Doctores = doctores
                    .OrderBy(d => d.Nombre, StringComparer.Create(TurnosIndexViewModel.Cultura, ignoreCase: true))
                    .Select(d => new SelectListItem(d.Nombre, $"{d.IdDoctor}", d.IdDoctor == filtroDoctor))
                    .ToList(),
                Movimientos = resultado.Movimientos
                    .Select(m => MovimientoFilaViewModel.Desde(m, NombreDeDoctor(m.IdDoctor, nombres)))
                    .ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // El 401 lo maneja ApiExceptionFilter mandando al login.
            return View("Index", new ActividadViewModel
            {
                Doctor = doctor,
                Accion = filtroAccion,
                Desde = desde,
                Hasta = hasta,
                Error = error.Message
            });
        }
    }

    /// <summary>
    /// "Mi actividad": lo de la agenda y el horario del doctor logueado. Acá
    /// no viaja ningún id de doctor: la API lo saca del token. De quien hizo
    /// cada cambio manda solo el rol.
    /// </summary>
    public async Task<IActionResult> Mia(string? accion, DateTime? desde, DateTime? hasta, int pagina = 1)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Mia)));

        if (!_auth.EsDoctor)
            return SinPermiso(TituloSinPermisoPropia, MotivoSinPermisoPropia, volverAlInicio: true);

        var filtroAccion = ActividadViewModel.EsAccionConocida(accion) ? accion : null;
        pagina = Math.Clamp(pagina, 1, PaginaMaxima);

        try
        {
            var resultado = await _movimientos.BuscarAsync(
                null, filtroAccion, desde, hasta, pagina, ActividadViewModel.PorPagina);

            if (PaginaFueraDeRango(resultado) is { } ultima)
            {
                return RedirectToAction(nameof(Mia),
                    new { accion = filtroAccion, desde = Dia(desde), hasta = Dia(hasta), pagina = EnRuta(ultima) });
            }

            return View("Index", new ActividadViewModel
            {
                EsPropia = true,
                Accion = filtroAccion,
                Desde = desde,
                Hasta = hasta,
                Pagina = pagina,
                Total = resultado.Total,
                Movimientos = resultado.Movimientos.Select(m => MovimientoFilaViewModel.Desde(m, null)).ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // 404: la cuenta es de doctor pero todavía no tiene perfil. No es
            // una falla, así que va como aviso y con el texto de la API.
            var sinPerfil = error.Status == StatusCodes.Status404NotFound;

            return View("Index", new ActividadViewModel
            {
                EsPropia = true,
                Accion = filtroAccion,
                Desde = desde,
                Hasta = hasta,
                Aviso = sinPerfil ? $"{error.Message} Pedile a administración que lo complete." : null,
                Error = sinPerfil ? null : error.Message
            });
        }
    }

    /// <summary>
    /// Una página más allá de la última (por la URL, o porque se achicó el
    /// filtro) devuelve la última que existe, para redirigir ahí. Null si la
    /// página pedida está bien o si directamente no hay movimientos.
    /// </summary>
    private static int? PaginaFueraDeRango(MovimientosPagina resultado) =>
        resultado.Movimientos.Count == 0 && resultado.Total > 0
            ? (int)Math.Ceiling(resultado.Total / (double)ActividadViewModel.PorPagina)
            : null;

    /// <summary>La página como viaja en la URL: la primera no se escribe.</summary>
    private static int? EnRuta(int pagina) => pagina > 1 ? pagina : null;

    /// <summary>Una fecha como viaja en la URL; null si no hay.</summary>
    private static string? Dia(DateTime? fecha) => fecha?.ToString("yyyy-MM-dd");

    /// <summary>
    /// El select solo trae doctores activos: un movimiento de uno que ya no
    /// está en la lista se muestra con su número.
    /// </summary>
    private static string? NombreDeDoctor(int? idDoctor, IReadOnlyDictionary<int, string> nombres) =>
        idDoctor is not { } id ? null
        : nombres.TryGetValue(id, out var nombre) && !string.IsNullOrWhiteSpace(nombre) ? nombre.Trim()
        : $"Doctor #{id}";
}
