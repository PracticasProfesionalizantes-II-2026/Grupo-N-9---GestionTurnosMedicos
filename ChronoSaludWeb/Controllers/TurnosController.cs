using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class TurnosController : ControladorBase
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

        var rol = _auth.SesionActual?.Rol;
        var filtros = new TurnosFiltroViewModel { Estado = estado, Desde = desde, Hasta = hasta };

        try
        {
            var ambito = await ResolverAmbitoAsync();

            // No se pudo determinar de quién son los turnos: no se muestra
            // ninguno. Nunca se cae a "mostrar todo".
            if (ambito.Bloqueado)
            {
                return View(new TurnosIndexViewModel
                {
                    Rol = rol,
                    Filtros = filtros,
                    Aviso = ambito.Bloqueo
                });
            }

            // paciente_id y doctor_id salen del ámbito, no de la query string:
            // el usuario no puede ampliarse el alcance desde la URL.
            var pagina = await _turnos.ObtenerAsync(
                pacienteId: ambito.PacienteId,
                doctorId:   ambito.DoctorId,
                estado:     filtros.Estado,
                desde:      filtros.Desde,
                hasta:      filtros.Hasta,
                limite:     Limite);

            return View(new TurnosIndexViewModel
            {
                Rol     = rol,
                Total   = pagina.Total,
                Turnos  = pagina.Turnos.Select(TurnoFilaViewModel.Desde).ToList(),
                Filtros = filtros
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // El 401 no se atrapa a propósito: lo maneja ApiExceptionFilter
            // mandando al login. El resto se muestra dentro de la página.
            return View(new TurnosIndexViewModel { Rol = rol, Error = error.Message, Filtros = filtros });
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        try
        {
            var modelo = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());

            // No existe, o existe pero no es del usuario: en los dos casos se
            // responde lo mismo, para no confirmar que el turno existe.
            if (modelo is null)
            {
                return NoEncontrado(id, "Turno no encontrado", "turno");
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
            var modelo = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());

            if (modelo is null)
            {
                return NoEncontrado(id, "Turno no encontrado", "turno");
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
            // Se revalida el ámbito antes de borrar: si no, alcanzaba con
            // postear el id de un turno ajeno.
            var ambito = await ResolverAmbitoAsync();
            var turno = await _turnos.ObtenerPorIdAsync(id);

            if (turno is null || !ambito.Incluye(turno.IdPaciente, turno.IdDoctor))
            {
                return NoEncontrado(id, "Turno no encontrado", "turno");
            }

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
    /// TurnoDto no manda. Null si no existe o si no entra en el ámbito.
    /// </summary>
    private async Task<TurnoDetalleViewModel?> ArmarDetalleAsync(int id, AmbitoTurnos ambito)
    {
        var turno = await _turnos.ObtenerPorIdAsync(id);
        if (turno is null) return null;

        // El turno existe pero no es del usuario: se trata igual que si no
        // existiera, y de paso nos ahorramos buscar los nombres.
        if (!ambito.Incluye(turno.IdPaciente, turno.IdDoctor)) return null;

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

    /// <summary>
    /// Qué turnos puede ver el usuario logueado. Los ids salen siempre de la
    /// sesión y del perfil que informa la API, nunca de la query string, así
    /// nadie puede ampliarse el alcance desde la URL.
    /// </summary>
    private sealed record AmbitoTurnos(int? PacienteId, int? DoctorId, string? Bloqueo)
    {
        /// <summary>No se pudo resolver el alcance: no se muestra ningún turno.</summary>
        public bool Bloqueado => Bloqueo is not null;

        /// <summary>Solo el administrador ve la agenda completa.</summary>
        public bool VeTodo => !Bloqueado && PacienteId is null && DoctorId is null;

        public bool Incluye(int idPaciente, int idDoctor) =>
            VeTodo || PacienteId == idPaciente || DoctorId == idDoctor;
    }

    private async Task<AmbitoTurnos> ResolverAmbitoAsync()
    {
        switch (_auth.SesionActual?.Rol)
        {
            case "administrador":
                return new AmbitoTurnos(null, null, null);

            case "paciente":
            {
                var idPaciente = await IdPerfilAsync(esDoctor: false);
                return idPaciente is null
                    ? new AmbitoTurnos(null, null,
                        "Tu usuario no tiene un perfil de paciente asociado, así que no podemos " +
                        "saber qué turnos son tuyos. Pedile a la administración que lo cree.")
                    : new AmbitoTurnos(idPaciente, null, null);
            }

            case "doctor":
            {
                var idDoctor = await IdPerfilAsync(esDoctor: true);
                return idDoctor is null
                    ? new AmbitoTurnos(null, null,
                        "Tu usuario tiene rol doctor pero no tiene un perfil de doctor cargado " +
                        "(matrícula, especialidad), así que no podemos saber qué turnos son tuyos.")
                    : new AmbitoTurnos(null, idDoctor, null);
            }

            default:
                // Rol inesperado: se deniega. Es preferible una pantalla vacía
                // antes que mostrarle la agenda de todos por descarte.
                return new AmbitoTurnos(null, null,
                    $"No hay un alcance definido para el rol \"{_auth.SesionActual?.Rol}\", " +
                    "así que no se muestran turnos.");
        }
    }

    /// <summary>
    /// IdPaciente o IdDoctor del usuario, que no son el IdUsuario. Se cachea en
    /// la sesión para no pedirlo en cada pantalla, pero solo cuando existe: si
    /// todavía no tiene perfil se vuelve a preguntar, así aparece apenas se lo
    /// crean sin necesidad de volver a loguearse.
    /// </summary>
    private async Task<int?> IdPerfilAsync(bool esDoctor)
    {
        var cacheado = HttpContext.Session.ObtenerIdPerfil();
        if (cacheado is not null) return cacheado;

        var id = esDoctor
            ? (await _doctores.ObtenerMiPerfilAsync())?.IdDoctor
            : (await _pacientes.ObtenerMiPerfilAsync())?.IdPaciente;

        if (id is not null) HttpContext.Session.GuardarIdPerfil(id.Value);
        return id;
    }
}
