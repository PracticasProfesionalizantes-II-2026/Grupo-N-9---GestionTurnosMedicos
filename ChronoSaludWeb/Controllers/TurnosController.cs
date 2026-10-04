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
    private readonly PerfilService _perfil;
    private readonly UsuarioService _usuarios;

    public TurnosController(
        TurnoService turnos,
        PacienteService pacientes,
        DoctorService doctores,
        AuthService auth,
        PerfilService perfil,
        UsuarioService usuarios)
    {
        _turnos = turnos;
        _pacientes = pacientes;
        _doctores = doctores;
        _auth = auth;
        _perfil = perfil;
        _usuarios = usuarios;
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

            // El listado no trae ids de persona, así que se pregunta por turno:
            // un solo pedido dice qué turnos tienen un paciente con foto.
            var fotos = await _usuarios.ObtenerFotosAsync(turnos: pagina.Turnos.Select(t => t.IdTurno));

            return View(new TurnosIndexViewModel
            {
                Rol     = rol,
                Total   = pagina.Total,
                Turnos  = pagina.Turnos
                    .Select(t => TurnoFilaViewModel.DesdeConFoto(t, UrlDeFoto(fotos.Turnos, t.IdTurno)))
                    .ToList(),
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
    /// Mueve el turno a "confirmado" o a "completado". La cancelación no pasa por
    /// acá: sigue yendo por DELETE (Cancelar), así hay un solo camino para cancelar.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string estado, string? volverA)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        if (!_auth.PuedeCambiarEstadoTurno)
            return SinPermiso(
                "No podés cambiar el estado de un turno con tu rol",
                "La API reserva PUT /turnos al personal: el doctor sobre sus propios turnos, " +
                "y administrador o secretario sobre cualquiera.");

        // El estado llega del formulario y la API lo guardaría tal cual, sin
        // validarlo, así que la lista blanca la ponemos nosotros.
        if (!EstadosQueSePuedenAplicar.Contains(estado))
        {
            TempData["Error"] = $"\"{estado}\" no es un estado que se pueda aplicar desde acá.";
            return VolverDe(volverA, id);
        }

        try
        {
            // Mismo motivo que en CancelarConfirmado: sin revalidar el ámbito
            // alcanzaba con postear el id de un turno ajeno.
            var ambito = await ResolverAmbitoAsync();
            var turno = await _turnos.ObtenerPorIdAsync(id);

            if (turno is null || !ambito.Incluye(turno.IdPaciente, turno.IdDoctor))
            {
                return NoEncontrado(id, "Turno no encontrado", "turno");
            }

            // Se valida contra el estado real que devolvió la API, no contra el que
            // tenía la página cuando se pintó: el turno pudo moverse mientras tanto.
            var actual = new TurnoDetalleViewModel { Estado = turno.Estado };

            var habilitado = estado == "confirmado"
                ? actual.PuedeConfirmarse
                : actual.PuedeCompletarse;

            if (!habilitado)
            {
                TempData["Error"] =
                    $"El turno #{id} está {turno.Estado} y no se puede pasar a {estado}.";
                return VolverDe(volverA, id);
            }

            await _turnos.CambiarEstadoAsync(id, estado);
            TempData["Exito"] = $"Turno #{id} marcado como {estado}.";
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // El 401 no se atrapa a propósito: lo maneja ApiExceptionFilter
            // mandando al login.
            TempData["Error"] = error.Message;
        }

        return VolverDe(volverA, id);
    }

    /// <summary>
    /// Los dos únicos estados que el front aplica por PUT. "cancelado" queda
    /// afuera porque va por DELETE, y "pendiente" porque no se vuelve atrás.
    /// </summary>
    private static readonly string[] EstadosQueSePuedenAplicar = ["confirmado", "completado"];

    /// <summary>
    /// Devuelve al usuario a donde estaba: al listado si apretó el botón desde
    /// ahí, al detalle del turno en cualquier otro caso.
    /// </summary>
    private IActionResult VolverDe(string? volverA, int id) =>
        volverA == nameof(Index)
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Detalle), new { id });

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
        var fotos    = await _usuarios.ObtenerFotosAsync(pacientes: new[] { turno.IdPaciente });

        return new TurnoDetalleViewModel
        {
            PacienteFotoUrl = UrlDeFoto(fotos.Pacientes, turno.IdPaciente),
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

    /// <summary>
    /// /Usuarios/Foto/{idUsuario} si la API informó una foto visible para ese
    /// id; null si no, así el avatar no pide una imagen que no existe.
    /// </summary>
    private string? UrlDeFoto(IReadOnlyDictionary<int, int> fotos, int id) =>
        fotos.TryGetValue(id, out var idUsuario)
            ? Url.Action("Foto", "Usuarios", new { id = idUsuario })
            : null;

    /// <summary>
    /// Pantalla de pedir turno. Cada paso (especialidad, doctor, fecha) es un
    /// formulario GET que vuelve acá con la elección en la query string, así
    /// el flujo no depende de JavaScript ni de la sesión.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Crear(int? paciente, string? especialidad, int? idDoctor, DateTime? fecha)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeCargarTurnos)
            return SinPermiso(
                "No podés cargar turnos con tu rol",
                "Para dar un turno hay que elegir el paciente de una lista, y la API solo se la " +
                "muestra a los roles doctor y administrador.");

        var rol = _auth.SesionActual?.Rol;
        var modelo = new TurnoCrearViewModel
        {
            Rol = rol,
            Especialidad = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim(),
            DoctorElegido = idDoctor
        };

        // Sin fecha, o con una ya pasada (escrita a mano en la URL), se busca
        // desde hoy: la API no da franjas para días anteriores.
        modelo.FechaInicio = fecha is { } elegida && elegida.Date >= modelo.Hoy ? elegida.Date : modelo.Hoy;

        if (rol == "paciente")
        {
            var idPaciente = await _perfil.IdPerfilAsync(esDoctor: false);
            if (idPaciente is null)
                return SinPermiso(
                    "Todavía no podés agendar turnos",
                    "Tu usuario no tiene un perfil de paciente asociado, así que no podemos crear " +
                    "el turno a tu nombre. Pedile a la administración que lo cree.");

            modelo.IdPaciente = idPaciente;
        }
        else if (paciente is not null)
        {
            // Se llega acá desde la ficha de un paciente ("Agendar turno"): se
            // precarga el select en vez de dejarlo en la opción vacía.
            modelo.IdPaciente = paciente;
        }

        await CargarListasAsync(modelo, incluirPacientes: rol != "paciente");
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

        var rol = _auth.SesionActual?.Rol;
        modelo.Rol = rol;

        if (rol == "paciente")
        {
            var idPaciente = await _perfil.IdPerfilAsync(esDoctor: false);
            if (idPaciente is null)
                return SinPermiso(
                    "Todavía no podés agendar turnos",
                    "Tu usuario no tiene un perfil de paciente asociado, así que no podemos crear " +
                    "el turno a tu nombre. Pedile a la administración que lo cree.");

            // El formulario del paciente no tiene selector: el IdPaciente sale
            // siempre del perfil logueado, nunca de lo que llegue posteado, así
            // nadie puede cargar un turno a nombre de otro paciente (la API no
            // lo controla: POST /turnos toma el IdPaciente del body tal cual).
            modelo.IdPaciente = idPaciente.Value;
            ModelState.Remove(nameof(modelo.IdPaciente));
        }

        // El doctor y las horas no tienen campos propios: salen del botón de la
        // franja que se tocó. Se sacan de ModelState los "obligatorio" que el
        // binding marcó al no encontrarlos en el formulario: la vista ya no
        // tiene dónde mostrarlos, así que el aviso va arriba como error general.
        ModelState.Remove(nameof(modelo.IdDoctor));
        ModelState.Remove(nameof(modelo.HoraInicio));
        ModelState.Remove(nameof(modelo.HoraFin));

        if (FranjaViewModel.TryParse(modelo.Franja, out var idDoctor, out var horaInicio, out var horaFin))
        {
            modelo.IdDoctor = idDoctor;
            modelo.HoraInicio = horaInicio;
            modelo.HoraFin = horaFin;
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Elegí uno de los horarios libres.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(modelo, incluirPacientes: rol != "paciente");
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
            await CargarListasAsync(modelo, incluirPacientes: rol != "paciente");
            return View(modelo);
        }
    }

    /// <summary>
    /// Llena los selects y las franjas libres. Si la API falla no tira: deja
    /// las listas vacías para no perder lo que el usuario venía cargando en el
    /// formulario. Un paciente no puede pedir GET /pacientes (reservado a
    /// doctor/administrador), así que para ese rol se salta ese fetch:
    /// <paramref name="incluirPacientes"/> en false.
    /// </summary>
    private async Task CargarListasAsync(TurnoCrearViewModel modelo, bool incluirPacientes = true)
    {
        IReadOnlyList<DoctorLista> doctores;

        try
        {
            if (incluirPacientes)
            {
                var pacientes = await _pacientes.ObtenerTodosAsync();
                modelo.Pacientes = pacientes
                    .Select(p => new SelectListItem($"{p.Nombre} {p.Apellido}".Trim(), p.IdPaciente.ToString()))
                    .ToList();
            }

            var especialidades = await _doctores.ObtenerEspecialidadesAsync();

            // Una especialidad que la API no conoce (escrita a mano en la URL,
            // o de un doctor que se dio de baja) se descarta en vez de buscar
            // con ella. Se guarda la versión de la API para que coincida el select.
            modelo.Especialidad = especialidades.FirstOrDefault(e =>
                string.Equals(e, modelo.Especialidad, StringComparison.OrdinalIgnoreCase));

            modelo.Especialidades = especialidades
                .Select(e => new SelectListItem(e, e, e == modelo.Especialidad))
                .ToList();

            if (modelo.Especialidad is null)
            {
                modelo.DoctorElegido = null;
                return;
            }

            // La API filtra por "contiene": sin este Where, "Cardiología"
            // traería también a los de "Cardiología infantil".
            doctores = (await _doctores.BuscarAsync(modelo.Especialidad, limite: 200)).Doctores
                .Where(d => string.Equals(d.Especialidad?.Trim(), modelo.Especialidad, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Nombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // Un doctor que no es de la especialidad (porque se cambió la
            // especialidad y se reenvió el formulario) vuelve a "Cualquiera".
            if (!doctores.Any(d => d.IdDoctor == modelo.DoctorElegido))
                modelo.DoctorElegido = null;

            modelo.Doctores = doctores
                .Select(d => new SelectListItem(
                    d.Nombre.Trim(),
                    d.IdDoctor.ToString(),
                    d.IdDoctor == modelo.DoctorElegido))
                .ToList();
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, $"No se pudieron cargar las listas: {error.Message}");
            return;
        }

        await CargarFranjasAsync(modelo, doctores);
    }

    /// <summary>
    /// Franjas libres del doctor elegido, o de todos los de la especialidad si
    /// se eligió "Cualquiera". Los pedidos van uno después del otro por el
    /// mismo motivo que en ArmarDetalleAsync: el ApiClient lee el token de
    /// HttpContext.Session, que no es seguro en concurrencia.
    /// </summary>
    private async Task CargarFranjasAsync(TurnoCrearViewModel modelo, IReadOnlyList<DoctorLista> doctores)
    {
        if (modelo.FechaInicio is not { } fecha)
            return;

        var dia = DateOnly.FromDateTime(fecha);

        try
        {
            if (modelo.DoctorElegido is { } idDoctor)
            {
                // El horario semanal es solo una ayuda para elegir la fecha: si
                // no llega, la pantalla sigue andando sin él.
                try
                {
                    var horarios = await _doctores.ObtenerHorariosAsync(idDoctor);
                    if (horarios is not null)
                        modelo.HorarioDoctor = HorarioSemanalViewModel.Desde(horarios);
                }
                catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
                {
                }

                var franjas = await _doctores.ObtenerDisponibilidadAsync(idDoctor, dia);
                modelo.Franjas = franjas
                    .Select(f => new FranjaViewModel { IdDoctor = idDoctor, HoraInicio = f.HoraInicio, HoraFin = f.HoraFin })
                    .ToList();
                return;
            }

            var todas = new List<FranjaViewModel>();

            foreach (var doctor in doctores)
            {
                try
                {
                    var franjas = await _doctores.ObtenerDisponibilidadAsync(doctor.IdDoctor, dia);
                    todas.AddRange(franjas.Select(f => new FranjaViewModel
                    {
                        IdDoctor = doctor.IdDoctor,
                        DoctorNombre = doctor.Nombre.Trim(),
                        HoraInicio = f.HoraInicio,
                        HoraFin = f.HoraFin
                    }));
                }
                catch (ApiException error) when (error.Status is StatusCodes.Status400BadRequest
                                                              or StatusCodes.Status404NotFound)
                {
                    // Se dio de baja entre que se armó la lista y este pedido:
                    // se lo saltea y se siguen mostrando los demás.
                }
            }

            // "HH:mm" ordena bien como texto.
            modelo.Franjas = todas
                .OrderBy(f => f.HoraInicio, StringComparer.Ordinal)
                .ThenBy(f => f.DoctorNombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            modelo.AvisoFranjas = $"No se pudieron cargar los horarios libres: {error.Message}";
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
                var idPaciente = await _perfil.IdPerfilAsync(esDoctor: false);
                return idPaciente is null
                    ? new AmbitoTurnos(null, null,
                        "Tu usuario no tiene un perfil de paciente asociado, así que no podemos " +
                        "saber qué turnos son tuyos. Pedile a la administración que lo cree.")
                    : new AmbitoTurnos(idPaciente, null, null);
            }

            case "doctor":
            {
                var idDoctor = await _perfil.IdPerfilAsync(esDoctor: true);
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

}
