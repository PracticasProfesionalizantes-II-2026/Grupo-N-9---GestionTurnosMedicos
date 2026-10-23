using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

/// <summary>
/// Reportes de turnos (GET /reportes/resumen): por estado, por especialidad y
/// por doctor, con el ausentismo. El administrador ve toda la clínica; el
/// doctor, solo lo suyo. Solo lectura, con descarga en CSV.
/// </summary>
public class ReportesController : ControladorBase
{
    private const string TituloSinPermiso = "No podés ver los reportes con tu rol";
    private const string MotivoSinPermiso =
        "Los reportes de turnos son para la administración y para cada doctor sobre su agenda.";

    private readonly ReporteService _reportes;
    private readonly DoctorService _doctores;
    private readonly AuthService _auth;

    public ReportesController(ReporteService reportes, DoctorService doctores, AuthService auth)
    {
        _reportes = reportes;
        _doctores = doctores;
        _auth = auth;
    }

    public async Task<IActionResult> Index(string? periodo, DateTime? desde, DateTime? hasta, int? doctor, string? especialidad)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.EsAdministrador && !_auth.EsDoctor)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso, volverAlInicio: true);

        return View("Index", await ArmarAsync(periodo, desde, hasta, doctor, especialidad));
    }

    /// <summary>
    /// Lo mismo que la pantalla, como archivo. Si no hay reporte (período
    /// mal elegido o la API falló), vuelve a la pantalla, que dice qué pasó.
    /// </summary>
    public async Task<IActionResult> Csv(string? periodo, DateTime? desde, DateTime? hasta, int? doctor, string? especialidad)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.EsAdministrador && !_auth.EsDoctor)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso, volverAlInicio: true);

        var modelo = await ArmarAsync(periodo, desde, hasta, doctor, especialidad);
        if (modelo.Reporte is not { } reporte)
            return RedirectToAction(nameof(Index), modelo.RutaActual());

        var contenido = ReporteCsv.Armar(reporte, modelo.TextoDelPeriodo, modelo.DeQuien, deUnDoctor: modelo.EsPropia || modelo.Doctor is not null);
        return File(ReporteCsv.Bytes(contenido), "text/csv; charset=utf-8",
            ReporteCsv.NombreDeArchivo(modelo.Periodo.Desde, modelo.Periodo.Hasta));
    }

    private async Task<ReportesViewModel> ArmarAsync(string? periodo, DateTime? desde, DateTime? hasta, int? doctor, string? especialidad)
    {
        var esPropia = !_auth.EsAdministrador;
        var elegido = PeriodoDeReporte.Resolver(periodo, desde, hasta, FechaArgentina.Hoy());

        try
        {
            // Los filtros de doctor y especialidad son del administrador: lo
            // que llega por la URL y no está en los selects queda en "todos".
            IReadOnlyList<SelectListItem> doctores = Array.Empty<SelectListItem>();
            IReadOnlyList<SelectListItem> especialidades = Array.Empty<SelectListItem>();
            int? filtroDoctor = null;
            string? filtroEspecialidad = null;

            if (!esPropia)
            {
                var lista = await _doctores.ObtenerTodosAsync();
                var nombres = await _doctores.ObtenerEspecialidadesAsync();

                filtroDoctor = doctor is { } id && lista.Any(d => d.IdDoctor == id) ? doctor : null;
                filtroEspecialidad = nombres.FirstOrDefault(e => string.Equals(e, especialidad?.Trim(), StringComparison.OrdinalIgnoreCase));

                doctores = lista
                    .OrderBy(d => d.Nombre, StringComparer.Create(TurnosIndexViewModel.Cultura, ignoreCase: true))
                    .Select(d => new SelectListItem(d.Nombre, $"{d.IdDoctor}", d.IdDoctor == filtroDoctor))
                    .ToList();
                especialidades = nombres
                    .Select(e => new SelectListItem(e, e, e == filtroEspecialidad))
                    .ToList();
            }

            var problema = PeriodoDeReporte.Problema(elegido);
            var reporte = problema is null
                ? await _reportes.ResumenAsync(elegido.Desde, elegido.Hasta, filtroDoctor, filtroEspecialidad)
                : null;

            return new ReportesViewModel
            {
                EsPropia = esPropia,
                Periodo = elegido,
                Doctor = filtroDoctor,
                Especialidad = filtroEspecialidad,
                Doctores = doctores,
                Especialidades = especialidades,
                ProblemaDelPeriodo = problema,
                Reporte = reporte
            };
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // 404 del doctor: la cuenta todavía no tiene perfil. No es una
            // falla, así que va como aviso y con el texto de la API. El 401
            // lo maneja ApiExceptionFilter mandando al login.
            var sinPerfil = esPropia && error.Status == StatusCodes.Status404NotFound;

            return new ReportesViewModel
            {
                EsPropia = esPropia,
                Periodo = elegido,
                Aviso = sinPerfil ? $"{error.Message} Pedile a administración que lo complete." : null,
                Error = sinPerfil ? null : error.Message
            };
        }
    }
}
