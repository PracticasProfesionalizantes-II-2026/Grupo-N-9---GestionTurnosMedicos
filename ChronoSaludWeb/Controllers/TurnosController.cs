using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class TurnosController : Controller
{
    // La API pagina de a 20 por defecto. Pedimos más para que el resumen de
    // arriba cuente sobre algo representativo mientras no haya paginado propio.
    private const int Limite = 100;

    private readonly TurnoService _turnos;
    private readonly PacienteService _pacientes;
    private readonly DoctorService _doctores;
    private readonly AuthService _auth;

    public TurnosController(
        TurnoService turnos,
        PacienteService pacientes,
        DoctorService doctores,
        AuthService auth)
    {
        _turnos = turnos;
        _pacientes = pacientes;
        _doctores = doctores;
        _auth = auth;
    }

    public async Task<IActionResult> Index(string? estado, DateTime? desde, DateTime? hasta)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        // Un estado que la API no conoce se ignora, así nadie fuerza la query.
        if (!string.IsNullOrWhiteSpace(estado) &&
            !TurnosFiltroViewModel.EstadosTurno.Contains(estado, StringComparer.OrdinalIgnoreCase))
        {
            estado = null;
        }

        var filtros = new TurnosFiltroViewModel { Estado = estado, Desde = desde, Hasta = hasta };

        try
        {
            var pagina = await _turnos.ObtenerAsync(
                estado: filtros.Estado,
                desde:  filtros.Desde,
                hasta:  filtros.Hasta,
                limite: Limite);

            return View(new TurnosIndexViewModel
            {
                Total   = pagina.Total,
                Turnos  = pagina.Turnos.Select(TurnoFilaViewModel.Desde).ToList(),
                Filtros = filtros
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // El 401 no se atrapa a propósito: lo maneja ApiExceptionFilter
            // mandando al login. El resto se muestra dentro de la página.
            return View(new TurnosIndexViewModel { Error = error.Message, Filtros = filtros });
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        try
        {
            var modelo = await ArmarDetalleAsync(id);

            // La API contestó 404: no es un error, es un turno que no existe.
            if (modelo is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("NoEncontrado", id);
            }

            return View(modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new TurnoDetalleViewModel { IdTurno = id, Error = error.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Cancelar(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Cancelar), new { id }));

        if (!_auth.PuedeCancelarTurnos)
            return SinPermiso(
                "No podés cancelar turnos con tu rol",
                "La cancelación está reservada al personal: doctor, administrador y secretario.");

        try
        {
            var modelo = await ArmarDetalleAsync(id);

            if (modelo is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("NoEncontrado", id);
            }

            // Ya está cancelado: no tiene sentido volver a preguntar.
            if (string.Equals(modelo.Estado, "cancelado", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = $"El turno #{id} ya estaba cancelado.";
                return RedirectToAction(nameof(Index));
            }

            return View(modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ActionName(nameof(Cancelar))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarConfirmado(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Cancelar), new { id }));

        if (!_auth.PuedeCancelarTurnos)
            return SinPermiso(
                "No podés cancelar turnos con tu rol",
                "La cancelación está reservada al personal: doctor, administrador y secretario.");

        try
        {
            await _turnos.CancelarAsync(id);
            TempData["Exito"] = $"Turno #{id} cancelado correctamente.";
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Trae el turno y le pega los nombres del paciente y del doctor, que
    /// TurnoDto no manda. Null si la API contestó 404.
    /// </summary>
    private async Task<TurnoDetalleViewModel?> ArmarDetalleAsync(int id)
    {
        var turno = await _turnos.ObtenerPorIdAsync(id);
        if (turno is null) return null;

        // Van uno después del otro y no en paralelo porque el ApiClient lee
        // el token de HttpContext.Session, que no es seguro en concurrencia.
        var paciente = await _pacientes.ObtenerPorIdAsync(turno.IdPaciente);
        var doctor   = await _doctores.ObtenerPorIdAsync(turno.IdDoctor);

        return new TurnoDetalleViewModel
        {
            IdTurno       = turno.IdTurno,
            FechaInicio   = turno.FechaInicio,
            HoraInicio    = turno.HoraInicio,
            HoraFin       = turno.HoraFin,
            Estado        = turno.Estado,
            Observaciones = turno.Observaciones,
            IdPaciente    = turno.IdPaciente,
            IdDoctor      = turno.IdDoctor,

            PacienteNombre = paciente is null ? null : $"{paciente.Nombre} {paciente.Apellido}",
            DoctorNombre   = doctor is null ? null : $"{doctor.Nombre} {doctor.Apellido}",
            Especialidad   = doctor?.Especialidad,
            Matricula      = doctor?.Matricula,
            Consultorio    = doctor?.Consultorio
        };
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeCargarTurnos)
            return SinPermiso(
                "No podés cargar turnos con tu rol",
                "Para dar un turno hay que elegir el paciente de una lista, y la API solo se la " +
                "muestra a los roles doctor y administrador.");

        var modelo = new TurnoCrearViewModel { FechaInicio = DateTime.Today };
        await CargarListasAsync(modelo);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(TurnoCrearViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeCargarTurnos)
            return SinPermiso(
                "No podés cargar turnos con tu rol",
                "Para dar un turno hay que elegir el paciente de una lista, y la API solo se la " +
                "muestra a los roles doctor y administrador.");

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(modelo);
            return View(modelo);
        }

        try
        {
            var creado = await _turnos.CrearAsync(new TurnoNuevo(
                modelo.IdPaciente!.Value,
                modelo.IdDoctor!.Value,
                modelo.FechaInicio!.Value,
                modelo.HoraInicio,
                modelo.HoraFin,
                string.IsNullOrWhiteSpace(modelo.Observaciones) ? null : modelo.Observaciones.Trim()));

            TempData["Exito"] = creado is null
                ? "Turno creado correctamente."
                : $"Turno #{creado.IdTurno} creado correctamente. Queda en estado {creado.Estado}.";

            return RedirectToAction(nameof(Index));
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // 409 por conflicto de horario, 400 por datos inválidos, 0 si la API
            // no responde. El mensaje va arriba del formulario y el modelo vuelve
            // a la vista con todo lo que el usuario había cargado.
            ModelState.AddModelError(string.Empty, error.Message);
            await CargarListasAsync(modelo);
            return View(modelo);
        }
    }

    /// <summary>
    /// Llena los selects. Si la API falla no tira: deja las listas vacías para
    /// no perder lo que el usuario venía cargando en el formulario.
    /// </summary>
    private async Task CargarListasAsync(TurnoCrearViewModel modelo)
    {
        try
        {
            var pacientes = await _pacientes.ObtenerTodosAsync();
            modelo.Pacientes = pacientes
                .Select(p => new SelectListItem($"{p.Nombre} {p.Apellido}".Trim(), p.IdPaciente.ToString()))
                .ToList();

            var doctores = await _doctores.ObtenerTodosAsync();
            modelo.Doctores = doctores
                .Select(d => new SelectListItem(
                    string.IsNullOrWhiteSpace(d.Especialidad) ? d.Nombre : $"{d.Nombre} · {d.Especialidad}",
                    d.IdDoctor.ToString()))
                .ToList();
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, $"No se pudieron cargar las listas: {error.Message}");
        }
    }

    private IActionResult SinPermiso(string titulo, string motivo)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Titulo"] = titulo;
        ViewData["Motivo"] = motivo;
        return View("SinPermiso", _auth.SesionActual?.Rol);
    }

    private IActionResult AlLogin(string? destino) =>
        RedirectToAction("Login", "Cuenta", new { returnUrl = destino });
}
