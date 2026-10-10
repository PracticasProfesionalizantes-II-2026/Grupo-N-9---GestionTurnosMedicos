using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class TurnosController : ControladorBase
{
    private readonly TurnoService _turnos;
    private readonly PacienteService _pacientes;
    private readonly DoctorService _doctores;
    private readonly AuthService _auth;
    private readonly PerfilService _perfil;
    private readonly UsuarioService _usuarios;
    private readonly ClinicaOpciones _clinica;

    public TurnosController(
        TurnoService turnos,
        PacienteService pacientes,
        DoctorService doctores,
        AuthService auth,
        PerfilService perfil,
        UsuarioService usuarios,
        IOptions<ClinicaOpciones> clinica)
    {
        _clinica = clinica.Value;
        _turnos = turnos;
        _pacientes = pacientes;
        _doctores = doctores;
        _auth = auth;
        _perfil = perfil;
        _usuarios = usuarios;
    }

    public async Task<IActionResult> Index(
        string? estado, DateTime? desde, DateTime? hasta, string? orden, string? dir, string? ver, int pagina = 1)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        // Un estado que la API no conoce se ignora, así nadie fuerza la query.
        if (string.IsNullOrWhiteSpace(estado) ||
            !TurnosFiltroViewModel.EstadosTurno.Contains(estado, StringComparer.OrdinalIgnoreCase))
        {
            estado = null;
        }

        // Lo mismo con la columna de orden: una que no existe queda en null y
        // se aplica el orden por defecto.
        orden = TurnosFiltroViewModel.Columnas
            .FirstOrDefault(c => string.Equals(c, orden, StringComparison.OrdinalIgnoreCase));

        var verTodos = string.Equals(ver, "todos", StringComparison.OrdinalIgnoreCase);
        var descendente = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

        // "Sin cerrar" es del personal: al paciente no le toca cerrar turnos,
        // así que para él el valor se ignora. En ese modo no cuentan ni el
        // estado ni las fechas de la URL.
        var sinCerrar = _auth.PuedeCambiarEstadoTurno
                        && string.Equals(ver, TurnosSinCerrar.Ver, StringComparison.OrdinalIgnoreCase);
        if (sinCerrar)
        {
            estado = null;
            desde = null;
            hasta = null;
            verTodos = false;
        }

        // En "todos", si no se eligió un orden, primero lo más nuevo.
        if (verTodos && orden is null && dir is null)
            descendente = true;

        var rol = _auth.SesionActual?.Rol;
        var filtros = new TurnosFiltroViewModel
        {
            Estado      = estado,
            Desde       = desde,
            Hasta       = hasta,
            Orden       = orden,
            Descendente = descendente,
            VerTodos    = verTodos,
            SinCerrar   = sinCerrar,
            Pagina      = Math.Max(pagina, 1)
        };

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

            // Al entrar, sin fechas elegidas, se ven los turnos de hoy en adelante.
            var desdeParaApi = filtros.SoloProximos ? FechaArgentina.Hoy() : filtros.Desde;

            // "Sin cerrar": pendientes o confirmados hasta ayer.
            var hastaParaApi = filtros.SinCerrar ? TurnosSinCerrar.Hasta(FechaArgentina.Hoy()) : filtros.Hasta;
            var estadosParaApi = filtros.SinCerrar ? TurnosSinCerrar.Estados : null;

            // paciente_id y doctor_id salen del ámbito, no de la query string:
            // el usuario no puede ampliarse el alcance desde la URL. El filtro,
            // el orden y la página los resuelve la API.
            var resultado = await _turnos.ObtenerAsync(
                pacienteId:  ambito.PacienteId,
                doctorId:    ambito.DoctorId,
                estado:      filtros.Estado,
                estados:     estadosParaApi,
                desde:       desdeParaApi,
                hasta:       hastaParaApi,
                orden:       filtros.OrdenParaApi,
                descendente: filtros.Descendente,
                pagina:      filtros.Pagina,
                limite:      TurnosIndexViewModel.PorPagina);

            // Se pidió una página que ya no existe (por ejemplo, después de
            // cancelar el único turno de la última): se va a la última que hay.
            var totalPaginas = PaginadorViewModel.ContarPaginas(resultado.Total, TurnosIndexViewModel.PorPagina);
            if (filtros.Pagina > totalPaginas)
                return RedirectToAction(nameof(Index), filtros.RutaDePagina(totalPaginas));

            // El listado no trae ids de persona, así que se pregunta por turno:
            // un solo pedido dice qué turnos tienen un paciente con foto.
            var fotos = await _usuarios.ObtenerFotosAsync(turnos: resultado.Turnos.Select(t => t.IdTurno));

            var turnos = resultado.Turnos
                .Select(t => TurnoFilaViewModel.DesdeConFoto(t, UrlDeFoto(fotos.Turnos, t.IdTurno)))
                .ToList();

            return View(new TurnosIndexViewModel
            {
                Rol     = rol,
                Total   = resultado.Total,
                Turnos  = turnos,
                Conteos = resultado.Conteos,
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

    /// <summary>
    /// "Agregar a mi calendario": descarga el turno como archivo .ics, con un
    /// aviso el día anterior y otro una hora antes. Mismo control que el
    /// detalle: solo se descargan los turnos que el usuario puede ver.
    /// </summary>
    public async Task<IActionResult> Calendario(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        try
        {
            var turno = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());

            if (turno is null)
                return NoEncontrado(id, "Turno no encontrado", "turno");

            // Un turno cancelado, completado o ausente ya no hace falta recordarlo.
            if (!turno.EstaEnPie)
            {
                TempData["Error"] = "Ese turno ya no está vigente, así que no hace falta agregarlo al calendario.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            var texto = CalendarioIcs.Armar(turno, _clinica.Direccion, DateTime.UtcNow);

            // text/calendar: el celular lo abre directo con su app de calendario.
            return File(Encoding.UTF8.GetBytes(texto), "text/calendar; charset=utf-8", $"turno-{id}.ics");
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }

    /// <summary>
    /// El comprobante del turno para imprimir, con letra grande: mucha gente
    /// prefiere tenerlo en papel o se lo lleva un familiar. Mismo control que
    /// el detalle. Sin JavaScript: se imprime con el menú del navegador, como
    /// la receta.
    /// </summary>
    public async Task<IActionResult> Comprobante(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Comprobante), new { id }));

        try
        {
            var turno = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());

            if (turno is null)
                return NoEncontrado(id, "Turno no encontrado", "turno");

            return View(new TurnoComprobanteViewModel
            {
                IdTurno = id,
                Turno = turno,
                Direccion = _clinica.TieneDireccion ? _clinica.Direccion.Trim() : null,
                GeneradoEl = FechaArgentina.Ahora()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new TurnoComprobanteViewModel { IdTurno = id, Error = error.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Cancelar(int id, string? volver)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Cancelar), new { id, volver }));

        if (!_auth.PuedeCancelarTurnos)
            return SinPermiso(
                "No podés cancelar turnos con tu rol",
                "Cancelan turnos los pacientes (los suyos), los doctores (los de su agenda) y la administración.");

        try
        {
            var modelo = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());

            if (modelo is null)
            {
                return NoEncontrado(id, "Turno no encontrado", "turno");
            }

            // Completado o ya cancelado: no se muestra la confirmación, porque
            // la API no lo va a cancelar.
            if (!modelo.PuedeCancelarse)
            {
                TempData["Error"] = MensajeNoSeCancela(id, modelo.Estado);
                return VolverAlListado(volver);
            }

            // La vista reenvía esta URL en el formulario. Solo se le pasa si es
            // local: una de afuera se descarta acá y no llega al HTML.
            ViewData["Volver"] = Url.IsLocalUrl(volver) ? volver : null;

            return View(modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return VolverAlListado(volver);
        }
    }

    [HttpPost]
    [ActionName(nameof(Cancelar))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarConfirmado(int id, string? volver)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Cancelar), new { id, volver }));

        if (!_auth.PuedeCancelarTurnos)
            return SinPermiso(
                "No podés cancelar turnos con tu rol",
                "Cancelan turnos los pacientes (los suyos), los doctores (los de su agenda) y la administración.");

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

            // Se mira el estado real que devolvió la API: el turno pudo
            // completarse o cancelarse después de abrir la confirmación.
            if (!new TurnoDetalleViewModel { Estado = turno.Estado }.PuedeCancelarse)
            {
                TempData["Error"] = MensajeNoSeCancela(id, turno.Estado);
                return VolverAlListado(volver);
            }

            await _turnos.CancelarAsync(id);
            TempData["Exito"] = $"Turno #{id} cancelado correctamente.";
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
        }

        return VolverAlListado(volver);
    }

    private static string MensajeNoSeCancela(int id, string estado) =>
        string.Equals(estado, "cancelado", StringComparison.OrdinalIgnoreCase)
            ? $"El turno #{id} ya estaba cancelado."
            : $"El turno #{id} está {estado} y no se puede cancelar.";

    /// <summary>
    /// Pregunta antes de marcar un turno como completado o ausente, porque
    /// después no se puede cambiar. El formulario de la confirmación manda a
    /// CambiarEstado, que vuelve a validar todo.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Marcar(int id, string? estado, string? volver)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Marcar), new { id, estado, volver }));

        if (!_auth.PuedeCambiarEstadoTurno)
            return SinPermiso(
                "No podés cambiar el estado de un turno con tu rol",
                "El estado de un turno lo cambia el personal: cada doctor en sus propios turnos, " +
                "y la administración en cualquiera.");

        // Solo una URL local vuelve al listado; si no, se vuelve al detalle.
        var volverValido = Url.IsLocalUrl(volver) ? volver : null;

        if (!TurnoMarcarViewModel.EsEstadoValido(estado))
        {
            TempData["Error"] = "Elegí si el turno se marca como completado o como ausente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        try
        {
            var turno = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());
            if (turno is null)
                return NoEncontrado(id, "Turno no encontrado", "turno");

            // Mismo criterio que los botones: en pie y con la hora ya llegada.
            if (!turno.PuedeCompletarse)
            {
                TempData["Error"] = turno.EstaEnPie
                    ? $"El turno #{id} todavía no empezó: se puede marcar como {estado} recién a la hora del turno."
                    : $"El turno #{id} está {turno.Estado} y no se puede pasar a {estado}.";
                return volverValido is null ? RedirectToAction(nameof(Detalle), new { id }) : LocalRedirect(volverValido);
            }

            return View(new TurnoMarcarViewModel { Turno = turno, Estado = estado!, Volver = volverValido });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }

    /// <summary>
    /// Mueve el turno a "confirmado", "completado" o "ausente". Los dos últimos
    /// llegan desde la confirmación (Marcar). La cancelación no pasa por acá:
    /// sigue yendo por DELETE (Cancelar), así hay un solo camino para cancelar.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string estado, string? volverA, string? volver)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        if (!_auth.PuedeCambiarEstadoTurno)
            return SinPermiso(
                "No podés cambiar el estado de un turno con tu rol",
                "El estado de un turno lo cambia el personal: cada doctor en sus propios turnos, " +
                "y la administración en cualquiera.");

        // El estado llega del formulario: además de lo que valida la API, la
        // lista blanca la ponemos nosotros.
        if (!EstadosQueSePuedenAplicar.Contains(estado))
        {
            TempData["Error"] = $"\"{estado}\" no es un estado que se pueda aplicar desde acá.";
            return VolverDe(volverA, id, volver);
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
            var actual = new TurnoDetalleViewModel
            {
                Estado = turno.Estado,
                FechaInicio = turno.FechaInicio,
                HoraInicio = turno.HoraInicio
            };

            var habilitado = false;
            if (estado == "confirmado") habilitado = actual.PuedeConfirmarse;
            if (estado == "completado") habilitado = actual.PuedeCompletarse;
            if (estado == "ausente")    habilitado = actual.PuedeMarcarAusente;

            if (!habilitado)
            {
                // Completado y ausente esperan a que llegue la hora del turno.
                TempData["Error"] = actual.EstaEnPie && !actual.YaEmpezo && estado != "confirmado"
                    ? $"El turno #{id} todavía no empezó: se puede marcar como {estado} recién a la hora del turno."
                    : $"El turno #{id} está {turno.Estado} y no se puede pasar a {estado}.";
                return VolverDe(volverA, id, volver);
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

        return VolverDe(volverA, id, volver);
    }

    /// <summary>
    /// Los estados que el front aplica por PUT. "cancelado" queda afuera
    /// porque va por DELETE, y "pendiente" porque no se vuelve atrás.
    /// </summary>
    private static readonly string[] EstadosQueSePuedenAplicar = ["confirmado", "completado", "ausente"];

    /// <summary>
    /// Devuelve al usuario a donde estaba: al listado si apretó el botón desde
    /// ahí, al detalle del turno en cualquier otro caso.
    /// </summary>
    private IActionResult VolverDe(string? volverA, int id, string? volver) =>
        volverA == nameof(Index)
            ? VolverAlListado(volver)
            : RedirectToAction(nameof(Detalle), new { id });

    /// <summary>
    /// Vuelve al listado tal como estaba (tarjeta activa, orden y fechas), que
    /// es la URL que el propio listado mandó en "volver". Solo se la sigue si
    /// es local: sin ese chequeo, un enlace armado podría mandar al usuario a
    /// otro sitio después de cancelar. Vacía o de afuera, se va al listado
    /// sin filtros, como antes.
    /// </summary>
    private IActionResult VolverAlListado(string? volver) =>
        Url.IsLocalUrl(volver)
            ? LocalRedirect(volver!)
            : RedirectToAction(nameof(Index));

    /// <summary>
    /// Reprogramar: elegir otro día y horario con el mismo doctor, en la misma
    /// tira de días que "pedir turno". Solo el personal: la API reserva PUT
    /// /turnos a doctor (sus turnos) y administrador.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Reprogramar(int id, DateTime? fecha, DateTime? desde)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Reprogramar), new { id }));

        if (!_auth.PuedeCambiarEstadoTurno)
            return SinPermisoParaReprogramar();

        TurnoDetalleViewModel? turno;
        try
        {
            turno = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        if (turno is null)
            return NoEncontrado(id, "Turno no encontrado", "turno");

        if (!turno.EstaEnPie)
        {
            TempData["Error"] = $"El turno #{id} está {turno.Estado}: solo se reprograma uno pendiente o confirmado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var modelo = new TurnoReprogramarViewModel { Turno = turno };

        try
        {
            modelo.Tira = await ArmarTiraAsync(
                new List<int> { turno.IdDoctor }, fecha, desde, nameof(Reprogramar), modelo.RutaBase);
            modelo.Fecha = modelo.Tira.DiaElegido;

            if (modelo.Fecha is { } dia)
            {
                var franjas = await _doctores.ObtenerDisponibilidadAsync(turno.IdDoctor, DateOnly.FromDateTime(dia));
                modelo.Franjas = franjas
                    .Select(f => new FranjaViewModel { IdDoctor = turno.IdDoctor, HoraInicio = f.HoraInicio, HoraFin = f.HoraFin })
                    .ToList();
            }
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            modelo.AvisoFranjas = $"No se pudieron cargar los horarios libres: {error.Message}";
        }

        return View(modelo);
    }

    /// <summary>
    /// Confirmación de la reprogramación: "de … a …". Todavía no cambió nada.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmarReprogramacion(int id, DateTime? fecha, string? inicio, string? fin)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Reprogramar), new { id }));

        if (!_auth.PuedeCambiarEstadoTurno)
            return SinPermisoParaReprogramar();

        // El enlace pudo armarse a mano: si el horario no tiene sentido, se
        // vuelve a elegir. Que siga libre lo controla la API al confirmar.
        if (fecha is null || fecha.Value.Date < FechaArgentina.Hoy() || !TurnoConfirmarViewModel.HorarioValido(inicio, fin))
        {
            TempData["Error"] = "Ese horario no es válido. Elegí uno de la lista.";
            return RedirectToAction(nameof(Reprogramar), new { id });
        }

        TurnoDetalleViewModel? turno;
        try
        {
            turno = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        if (turno is null)
            return NoEncontrado(id, "Turno no encontrado", "turno");

        if (!turno.EstaEnPie)
        {
            TempData["Error"] = $"El turno #{id} está {turno.Estado}: solo se reprograma uno pendiente o confirmado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        return View(new ConfirmarReprogramacionViewModel
        {
            Turno = turno,
            Fecha = fecha.Value.Date,
            HoraInicio = inicio!,
            HoraFin = fin!
        });
    }

    [HttpPost]
    [ActionName(nameof(ConfirmarReprogramacion))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarReprogramacionPost(int id, DateTime? fecha, string? inicio, string? fin)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Reprogramar), new { id }));

        if (!_auth.PuedeCambiarEstadoTurno)
            return SinPermisoParaReprogramar();

        if (fecha is null || !TurnoConfirmarViewModel.HorarioValido(inicio, fin))
        {
            TempData["Error"] = "Ese horario no es válido. Elegí uno de la lista.";
            return RedirectToAction(nameof(Reprogramar), new { id });
        }

        TurnoDetalleViewModel? turno = null;
        try
        {
            // Se revalida el ámbito antes de cambiar nada: si no, alcanzaba
            // con postear el id de un turno ajeno.
            turno = await ArmarDetalleAsync(id, await ResolverAmbitoAsync());
            if (turno is null)
                return NoEncontrado(id, "Turno no encontrado", "turno");

            await _turnos.ReprogramarAsync(id, fecha.Value.Date, inicio!, fin!);

            var nuevo = new ConfirmarReprogramacionViewModel { Turno = turno, Fecha = fecha.Value.Date, HoraInicio = inicio!, HoraFin = fin! };
            TempData["Exito"] =
                $"Turno #{id} reprogramado: del {nuevo.FechaActualLarga} a las {turno.HoraInicio} " +
                $"al {nuevo.FechaNuevaLarga} a las {nuevo.HoraInicio}. Se le avisó al paciente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // 409: el horario se ocupó mientras tanto. 400: por ejemplo, ya
            // pasó o quedó fuera del horario del doctor.
            if (turno is null)
            {
                TempData["Error"] = error.Message;
                return RedirectToAction(nameof(Detalle), new { id });
            }

            return View(nameof(ConfirmarReprogramacion), new ConfirmarReprogramacionViewModel
            {
                Turno = turno,
                Fecha = fecha.Value.Date,
                HoraInicio = inicio!,
                HoraFin = fin!,
                Error = error.Message
            });
        }
    }

    private IActionResult SinPermisoParaReprogramar() =>
        SinPermiso(
            "No podés reprogramar turnos con tu rol",
            "Los turnos los reprograma el personal: cada doctor los de su agenda y la administración cualquiera.");

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
    /// Pantalla de pedir turno, en tres pasos con enlaces: especialidad,
    /// doctor (o "Cualquiera") y día con horario (ver TurnoCrearViewModel).
    /// Cada enlace vuelve acá con la elección en la query string, así el flujo
    /// no depende de JavaScript ni de la sesión. Todavía no se reserva nada:
    /// el horario lleva a Confirmar.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Crear(
        int? paciente, string? especialidad, int? idDoctor, DateTime? fecha, DateTime? desde)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeCargarTurnos)
            return SinPermisoParaCargar();

        var rol = _auth.SesionActual?.Rol;

        if (rol == "paciente" && await _perfil.IdPerfilAsync(esDoctor: false) is null)
            return SinPerfilDePaciente();

        var modelo = new TurnoCrearViewModel
        {
            Rol = rol,
            // El paciente de la ficha ("Agendar turno") es cosa del personal:
            // el paciente siempre reserva a su nombre.
            IdPaciente = rol == "paciente" ? null : paciente,
            Especialidad = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim(),
            IdDoctor = idDoctor
        };

        try
        {
            await CargarPasosAsync(modelo, fecha, desde);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            modelo.AvisoFranjas = $"No se pudieron cargar los datos para pedir el turno: {error.Message}";
        }

        return View(modelo);
    }

    /// <summary>
    /// Confirmación del turno: se llega tocando un horario en Crear. Muestra
    /// el resumen y pide las observaciones (y el paciente, si es el personal).
    /// Recién al confirmar se le pide el turno a la API.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Confirmar(
        int idDoctor, DateTime? fecha, string? inicio, string? fin, string? especialidad, int? elegido, int? paciente,
        string? buscar)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeCargarTurnos)
            return SinPermisoParaCargar();

        var rol = _auth.SesionActual?.Rol;
        var modelo = new TurnoConfirmarViewModel
        {
            Rol = rol,
            IdDoctor = idDoctor,
            Fecha = fecha?.Date,
            HoraInicio = inicio ?? string.Empty,
            HoraFin = fin ?? string.Empty,
            Especialidad = especialidad,
            DoctorElegido = elegido,
            IdPaciente = rol == "paciente" ? null : paciente
        };

        // El enlace pudo armarse a mano: si el horario no tiene sentido, se
        // vuelve a elegir. Que siga libre lo controla la API al confirmar.
        if (idDoctor <= 0 || modelo.Fecha is null || modelo.Fecha < FechaArgentina.Hoy() ||
            !TurnoConfirmarViewModel.HorarioValido(inicio, fin))
        {
            TempData["Error"] = "Ese horario no es válido. Elegí uno de la lista.";
            return RedirectToAction(nameof(Crear), modelo.RutaCambiarHorario);
        }

        if (rol == "paciente")
        {
            var idPaciente = await _perfil.IdPerfilAsync(esDoctor: false);
            if (idPaciente is null)
                return SinPerfilDePaciente();

            modelo.IdPaciente = idPaciente;
        }

        await CargarConfirmacionAsync(modelo, buscar);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(TurnoConfirmarViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeCargarTurnos)
            return SinPermisoParaCargar();

        var rol = _auth.SesionActual?.Rol;
        modelo.Rol = rol;

        if (rol == "paciente")
        {
            var idPaciente = await _perfil.IdPerfilAsync(esDoctor: false);
            if (idPaciente is null)
                return SinPerfilDePaciente();

            // El IdPaciente sale siempre del perfil logueado, nunca de lo que
            // llegue posteado: nadie carga un turno a nombre de otro paciente.
            modelo.IdPaciente = idPaciente.Value;
            ModelState.Remove(nameof(modelo.IdPaciente));
        }

        if (!ModelState.IsValid)
        {
            await CargarConfirmacionAsync(modelo);
            return View(modelo);
        }

        try
        {
            var creado = await _turnos.CrearAsync(new TurnoNuevo(
                modelo.IdPaciente!.Value,
                modelo.IdDoctor,
                modelo.Fecha!.Value,
                modelo.HoraInicio,
                modelo.HoraFin,
                string.IsNullOrWhiteSpace(modelo.Observaciones) ? null : modelo.Observaciones.Trim()));

            if (creado is null)
            {
                TempData["Exito"] = "Turno reservado.";
                return RedirectToAction(nameof(Index));
            }

            // Al paciente el estado se le dice con palabras: "Queda esperando confirmación".
            var estado = EstadoTurnoTexto.Para(creado.Estado, rol == "paciente").ToLowerInvariant();
            TempData["Exito"] =
                $"Turno #{creado.IdTurno} reservado para el {modelo.FechaLarga}, de {modelo.Horario}. Queda {estado}.";
            return RedirectToAction(nameof(Detalle), new { id = creado.IdTurno });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // 409: el horario se ocupó mientras se confirmaba. 400: por
            // ejemplo, ya pasó. El mensaje va arriba y la vista ofrece elegir
            // otro horario, sin perder las observaciones.
            ModelState.AddModelError(string.Empty, error.Message);
            await CargarConfirmacionAsync(modelo);
            return View(modelo);
        }
    }

    private IActionResult SinPermisoParaCargar() =>
        SinPermiso(
            "No podés cargar turnos con tu rol",
            "Pueden pedir turnos los pacientes (a su nombre), los doctores y la administración.");

    private IActionResult SinPerfilDePaciente() =>
        SinPermiso(
            "Todavía no podés agendar turnos",
            "Tu usuario no tiene un perfil de paciente asociado, así que no podemos crear " +
            "el turno a tu nombre. Pedile a la administración que lo cree.");

    /// <summary>
    /// Carga lo que necesita el paso en el que está la pantalla. Lo que llegó
    /// en la URL se revisa contra la API: una especialidad o un doctor que no
    /// existen hacen volver al paso anterior en vez de buscar con ellos.
    /// </summary>
    private async Task CargarPasosAsync(TurnoCrearViewModel modelo, DateTime? fecha, DateTime? desde)
    {
        // Paso 1: las especialidades. Se guarda la versión de la API, así
        // coincide con la lista aunque en la URL venga con otras mayúsculas.
        modelo.Especialidades = await _doctores.ObtenerEspecialidadesAsync();
        modelo.Especialidad = modelo.Especialidades.FirstOrDefault(e =>
            string.Equals(e, modelo.Especialidad, StringComparison.OrdinalIgnoreCase));

        if (modelo.Especialidad is null)
        {
            modelo.IdDoctor = null;
            return;
        }

        // Paso 2: los doctores de la especialidad. La API filtra por
        // "contiene": sin este Where, "Cardiología" traería también a los de
        // "Cardiología infantil".
        modelo.Doctores = (await _doctores.BuscarAsync(modelo.Especialidad, limite: 200)).Doctores
            .Where(d => string.Equals(d.Especialidad?.Trim(), modelo.Especialidad, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        // Un doctor que no es de la especialidad vuelve a la elección del doctor.
        var esDeLaEspecialidad = modelo.Doctores.Any(d => d.IdDoctor == modelo.IdDoctor);
        if (!modelo.CualquierDoctor && !esDeLaEspecialidad)
            modelo.IdDoctor = null;

        if (modelo.IdDoctor is null || modelo.Doctores.Count == 0)
            return;

        // Paso 3. El horario semanal es solo una ayuda: si no llega, la
        // pantalla sigue andando sin él.
        if (!modelo.CualquierDoctor)
        {
            try
            {
                var horarios = await _doctores.ObtenerHorariosAsync(modelo.IdDoctor.Value);
                if (horarios is not null)
                    modelo.HorarioDoctor = HorarioSemanalViewModel.Desde(horarios);
            }
            catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
            {
            }

            // Sin horario cargado no hay días que mostrar: lo explica la vista.
            if (modelo.HorarioDoctor is { TieneHorario: false })
                return;
        }

        var idsDoctor = new List<int>();
        if (modelo.CualquierDoctor)
            idsDoctor.AddRange(modelo.Doctores.Select(d => d.IdDoctor));
        else
            idsDoctor.Add(modelo.IdDoctor.Value);

        modelo.Tira = await ArmarTiraAsync(
            idsDoctor, fecha, desde, nameof(Crear), modelo.Ruta(modelo.Especialidad, modelo.IdDoctor));
        modelo.Fecha = modelo.Tira.DiaElegido;

        if (modelo.Fecha is not null)
            await CargarFranjasAsync(modelo);
    }

    /// <summary>
    /// La tira de dos semanas con cuántos horarios libres tiene cada día,
    /// sumando los de los doctores de <paramref name="idsDoctor"/> (uno solo,
    /// o todos los de la especialidad con "Cualquiera"). Si no se eligió un
    /// día (o el elegido no está en la tira), queda elegido el primero con
    /// lugar. La usan "pedir turno" y "reprogramar": cada una le pasa su
    /// acción y su <paramref name="rutaBase"/>, lo que sus enlaces tienen que
    /// conservar.
    /// </summary>
    private async Task<TiraDeDiasViewModel> ArmarTiraAsync(
        IReadOnlyList<int> idsDoctor, DateTime? fecha, DateTime? desde, string accion, IDictionary<string, string> rutaBase)
    {
        const int dias = TurnoCrearViewModel.DiasDeLaTira;
        var hoy = FechaArgentina.Hoy();

        // La tira arranca en "desde" (si no es un día pasado) o en hoy. Si el
        // día elegido queda afuera, arranca en ese día.
        var inicio = hoy;
        if (desde is { } pedidoDesde && pedidoDesde.Date > hoy)
            inicio = pedidoDesde.Date;
        if (fecha is { } pedida && pedida.Date >= hoy && (pedida.Date < inicio || pedida.Date >= inicio.AddDays(dias)))
            inicio = pedida.Date;

        var libres = await ContarLibresPorDiaAsync(idsDoctor, inicio);

        // El día elegido: el de la URL si está en la tira; si no, el primero
        // con lugar (o el primero, si la API no informó cuántos hay).
        DateTime? elegido = null;
        if (fecha is { } enLaUrl && enLaUrl.Date >= inicio && enLaUrl.Date < inicio.AddDays(dias))
            elegido = enLaUrl.Date;

        for (var i = 0; i < dias && elegido is null; i++)
        {
            var dia = inicio.AddDays(i);
            if (libres is null || libres.GetValueOrDefault(dia) > 0)
                elegido = dia;
        }

        var lista = new List<DiaDeLaTira>();
        for (var i = 0; i < dias; i++)
        {
            var dia = inicio.AddDays(i);
            lista.Add(new DiaDeLaTira
            {
                Fecha = dia,
                Libres = libres is null ? null : libres.GetValueOrDefault(dia),
                Elegido = dia == elegido,
                Ruta = TiraDeDiasViewModel.RutaConFechas(rutaBase, dia, inicio)
            });
        }

        var anteriores = inicio.AddDays(-dias);
        return new TiraDeDiasViewModel
        {
            Accion = accion,
            Dias = lista,
            // Antes de hoy no hay nada para reservar.
            RutaAnteriores = inicio > hoy
                ? TiraDeDiasViewModel.RutaConFechas(rutaBase, null, anteriores < hoy ? hoy : anteriores)
                : null,
            RutaSiguientes = TiraDeDiasViewModel.RutaConFechas(rutaBase, null, inicio.AddDays(dias))
        };
    }

    /// <summary>
    /// Por día, cuántos horarios libres hay sumando los de esos doctores.
    /// Null si no se pudo saber de ninguno (por ejemplo, la API todavía no
    /// tiene dias-disponibles durante un despliegue): la tira se muestra sin
    /// números. Los pedidos van uno después del otro: el ApiClient lee el
    /// token de HttpContext.Session, que no es seguro en concurrencia.
    /// </summary>
    private async Task<Dictionary<DateTime, int>?> ContarLibresPorDiaAsync(IReadOnlyList<int> idsDoctor, DateTime inicio)
    {
        var conteo = new Dictionary<DateTime, int>();
        var respondieron = 0;

        foreach (var id in idsDoctor)
        {
            try
            {
                var dias = await _doctores.ObtenerDiasDisponiblesAsync(id, DateOnly.FromDateTime(inicio), TurnoCrearViewModel.DiasDeLaTira);
                respondieron++;

                foreach (var dia in dias)
                {
                    conteo.TryGetValue(dia.Fecha.Date, out var suma);
                    conteo[dia.Fecha.Date] = suma + dia.Libres;
                }
            }
            catch (ApiException error) when (error.Status is StatusCodes.Status400BadRequest
                                                          or StatusCodes.Status404NotFound)
            {
                // Un doctor que se dio de baja recién, o una API sin este
                // pedido: se lo saltea y se siguen sumando los demás.
            }
        }

        return respondieron == 0 ? null : conteo;
    }

    /// <summary>
    /// Horarios libres del día elegido, del doctor elegido o de todos los de
    /// la especialidad si es "Cualquiera". Los pedidos van uno después del
    /// otro, por lo mismo que en ContarLibresPorDiaAsync.
    /// </summary>
    private async Task CargarFranjasAsync(TurnoCrearViewModel modelo)
    {
        var dia = DateOnly.FromDateTime(modelo.Fecha!.Value);

        try
        {
            if (!modelo.CualquierDoctor)
            {
                var idDoctor = modelo.IdDoctor!.Value;
                var franjas = await _doctores.ObtenerDisponibilidadAsync(idDoctor, dia);
                modelo.Franjas = franjas
                    .Select(f => new FranjaViewModel { IdDoctor = idDoctor, HoraInicio = f.HoraInicio, HoraFin = f.HoraFin })
                    .ToList();
                return;
            }

            var todas = new List<FranjaViewModel>();

            foreach (var doctor in modelo.Doctores)
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
    /// Lo que la confirmación muestra además del horario: el doctor y, para
    /// el personal, el buscador de pacientes (<paramref name="buscar"/> es lo
    /// que se escribió ahí). Si la API falla no tira: la pantalla se muestra
    /// igual, con el aviso arriba.
    /// </summary>
    private async Task CargarConfirmacionAsync(TurnoConfirmarViewModel modelo, string? buscar = null)
    {
        try
        {
            var doctor = await _doctores.ObtenerPorIdAsync(modelo.IdDoctor);
            if (doctor is not null)
            {
                modelo.DoctorNombre = $"{doctor.Nombre} {doctor.Apellido}".Trim();
                modelo.DoctorEspecialidad = doctor.Especialidad;
                modelo.Consultorio = doctor.Consultorio;
            }

            // El buscador vuelve a esta misma pantalla con el horario elegido.
            if (modelo.MostrarSelectorPaciente)
            {
                var seleccion = await _pacientes.ElegirAsync(modelo.IdPaciente, buscar);
                modelo.IdPaciente = seleccion.Elegido?.IdPaciente;
                modelo.ElegirPaciente = ElegirPacienteViewModel.Desde(
                    seleccion, "Turnos", nameof(Confirmar), buscar, modelo.RutaConfirmar);
            }
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, $"No se pudieron cargar todos los datos: {error.Message}");
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
