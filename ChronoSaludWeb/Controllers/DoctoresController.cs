using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class DoctoresController : ControladorBase
{
    private const int Limite = 100;

    private readonly DoctorService _doctores;
    private readonly HorarioService _horarios;
    private readonly UsuarioService _usuarios;
    private readonly AuthService _auth;

    public DoctoresController(
        DoctorService doctores, HorarioService horarios, UsuarioService usuarios, AuthService auth)
    {
        _doctores = doctores;
        _horarios = horarios;
        _usuarios = usuarios;
        _auth = auth;
    }

    private const string TituloSinPermiso = "No podés ver los doctores con tu rol";

    // Hoy PuedeVerDoctores es true para cualquier sesión (GET /doctores no le
    // exige rol a la API), así que este mensaje es inalcanzable en la práctica.
    // Se deja el guard igual que en PacientesController para que, si el día de
    // mañana la API restringe el endpoint, el cambio sea solo en AuthService.
    private const string MotivoSinPermiso =
        "Tu usuario no tiene permiso para ver el padrón de doctores.";

    private const string TituloSinPermisoHorario = "No podés editar horarios con tu rol";
    private const string MotivoSinPermisoHorario =
        "Cargar y editar el horario de atención de un doctor está reservado a los administradores.";

    // La API contesta el 403 con el cuerpo vacío, así que el mensaje se arma acá.
    private const string SinPermisoDeLaApi =
        "No pudimos guardar el horario porque tu sesión no tiene permiso de administrador. " +
        "Cerrá la sesión y volvé a ingresar.";

    public async Task<IActionResult> Index(string? especialidad)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedeVerDoctores)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var filtro = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim();

        try
        {
            var especialidades = await _doctores.ObtenerEspecialidadesAsync();
            var pagina = await _doctores.BuscarAsync(filtro, Limite);

            // Un solo pedido para toda la página: quiénes tienen foto y cuál es
            // su IdUsuario, que el listado de doctores no trae.
            var fotos = await _usuarios.ObtenerFotosAsync(doctores: pagina.Doctores.Select(d => d.IdDoctor));

            return View(new DoctoresIndexViewModel
            {
                Especialidad = filtro,
                Total = pagina.Total,
                Doctores = pagina.Doctores
                    .Select(d => Mapear(d, UrlDeFoto(fotos, d.IdDoctor), _auth.EsAdministrador))
                    .ToList(),
                Especialidades = OpcionesDeEspecialidad(especialidades, filtro)
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new DoctoresIndexViewModel { Especialidad = filtro, Error = error.Message });
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        if (!_auth.PuedeVerDoctores)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        try
        {
            var doctor = await _doctores.ObtenerPorIdAsync(id);

            if (doctor is null)
                return NoEncontrado(id, "Doctor no encontrado", "doctor");

            // El horario va aparte: si falla, la ficha se muestra igual con
            // el aviso en su tarjeta en vez de perder todos los datos.
            HorarioSemanalViewModel? horario = null;
            string? errorHorario = null;
            try
            {
                var horarios = await _doctores.ObtenerHorariosAsync(id);
                horario = HorarioSemanalViewModel.Desde(horarios ?? Array.Empty<HorarioLaboral>());
            }
            catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
            {
                errorHorario = error.Message;
            }

            var fotos = await _usuarios.ObtenerFotosAsync(doctores: new[] { doctor.IdDoctor });

            return View(new DoctorDetalleViewModel
            {
                IdDoctor = doctor.IdDoctor,
                FotoUrl = UrlDeFoto(fotos, doctor.IdDoctor),
                Nombre = doctor.Nombre,
                Apellido = doctor.Apellido,
                Especialidad = doctor.Especialidad,
                Matricula = doctor.Matricula,
                Consultorio = doctor.Consultorio,
                Horario = horario,
                ErrorHorario = errorHorario,
                PuedePedirTurno = _auth.PuedeCargarTurnos,
                PuedeEditarHorario = _auth.PuedeEditarHorarios
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new DoctorDetalleViewModel { IdDoctor = id, Error = error.Message });
        }
    }

    /// <summary>
    /// Pantalla para cargar o editar el horario semanal de un doctor: siete
    /// filas, de lunes a domingo, con lo que tiene cargado hoy. Con
    /// <paramref name="estandar"/> las filas llegan con el horario estándar en
    /// lugar del actual; es solo una precarga, no guarda nada.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Horario(int id, bool estandar = false)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Horario), new { id }));

        if (!_auth.PuedeEditarHorarios)
            return SinPermiso(TituloSinPermisoHorario, MotivoSinPermisoHorario);

        try
        {
            var doctor = await _doctores.ObtenerPorIdAsync(id);
            var horarios = doctor is null ? null : await _doctores.ObtenerHorariosAsync(id);

            if (doctor is null || horarios is null)
                return NoEncontrado(id, "Doctor no encontrado", "doctor");

            return View(new HorarioEditarViewModel
            {
                IdDoctor = doctor.IdDoctor,
                NombreDoctor = NombreDe(doctor),
                Dias = estandar
                    ? HorarioEditarViewModel.DiasEstandar()
                    : HorarioEditarViewModel.DiasDesde(horarios),
                EsEstandar = estandar
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // Sin el horario actual a la vista no se ofrece el formulario:
            // guardarlo en blanco pisaría el que el doctor ya tiene.
            TempData["Error"] = $"No se pudo cargar el horario para editarlo: {error.Message}";
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }

    /// <summary>
    /// Guarda la semana completa en un solo envío. Del formulario solo se
    /// toman las filas y la confirmación; el doctor sale de la ruta.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Horario(
        int id,
        [Bind(nameof(HorarioEditarViewModel.Dias), nameof(HorarioEditarViewModel.ConfirmaSinHorario))]
        HorarioEditarViewModel modelo)
    {
        // Los mismos dos chequeos que el GET, antes de mirar el formulario:
        // que el botón no se muestre no frena a quien arma el POST a mano.
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Horario), new { id }));

        if (!_auth.PuedeEditarHorarios)
            return SinPermiso(TituloSinPermisoHorario, MotivoSinPermisoHorario);

        // Las filas son siete y fijas, porque el día sale de la posición. Con
        // otra cantidad el formulario no es el nuestro: no se interpreta.
        if (modelo.Dias.Count != HorarioEditarViewModel.DiasPorSemana)
        {
            TempData["Error"] = "El formulario del horario llegó incompleto. Cargalo de nuevo.";
            return RedirectToAction(nameof(Horario), new { id });
        }

        modelo.IdDoctor = id;

        try
        {
            var doctor = await _doctores.ObtenerPorIdAsync(id);
            if (doctor is null)
                return NoEncontrado(id, "Doctor no encontrado", "doctor");

            modelo.NombreDoctor = NombreDe(doctor);

            if (!ModelState.IsValid)
                return View(modelo);

            var nuevo = modelo.AHorarios();

            // Ningún día marcado deja al doctor sin horario y sin turnos: no se
            // guarda hasta que lo confirmen con el botón aparte.
            if (nuevo.Count == 0 && !modelo.ConfirmaSinHorario)
            {
                modelo.PideConfirmarSinHorario = true;
                return View(modelo);
            }

            // El botón de confirmar solo vale para la semana vacía. Si llegó
            // junto con días marcados no se adivina cuál de los dos se quiso.
            if (nuevo.Count > 0 && modelo.ConfirmaSinHorario)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Marcaste días de atención y a la vez elegiste dejar al doctor sin horario. " +
                    "No se guardó nada: destildá esos días, o usá \"Guardar horario\".");
                return View(modelo);
            }

            var resultado = await _horarios.GuardarAsync(id, nuevo);

            // Hay turnos reservados que quedarían fuera del horario: no se
            // guardó nada. Se listan sobre el formulario, que vuelve con lo cargado.
            if (!resultado.Guardado)
            {
                modelo.Conflictos = resultado.Conflictos.Select(TurnoFilaViewModel.Desde).ToList();
                return View(modelo);
            }

            TempData["Exito"] = nuevo.Count == 0
                ? $"{modelo.NombreDoctor} quedó sin horario de atención."
                : $"Horario de atención de {modelo.NombreDoctor} actualizado.";

            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // 400 si el doctor está inactivo o la API rechaza un rango, 0 si no
            // responde. El mensaje va arriba y el formulario vuelve con lo cargado.
            ModelState.AddModelError(
                string.Empty,
                error.Status == StatusCodes.Status403Forbidden ? SinPermisoDeLaApi : error.Message);
            return View(modelo);
        }
    }

    private static string NombreDe(DoctorDetalle doctor) =>
        string.IsNullOrWhiteSpace($"{doctor.Nombre}{doctor.Apellido}")
            ? "Este doctor"
            : $"{doctor.Nombre} {doctor.Apellido}".Trim();

    /// <summary>
    /// /Usuarios/Foto/{idUsuario} si la API informó que ese doctor tiene foto;
    /// null si no, así el avatar no pide una imagen que no existe.
    /// </summary>
    private string? UrlDeFoto(FotosDisponibles fotos, int idDoctor) =>
        fotos.Doctores.TryGetValue(idDoctor, out var idUsuario)
            ? Url.Action("Foto", "Usuarios", new { id = idUsuario })
            : null;

    private static DoctorFilaViewModel Mapear(DoctorLista doctor, string? fotoUrl, bool avisarSinHorario) => new()
    {
        FotoUrl = fotoUrl,
        IdDoctor = doctor.IdDoctor,
        Nombre = doctor.Nombre,
        Especialidad = doctor.Especialidad,
        Matricula = doctor.Matricula,
        // Solo cuando la API dice que no tiene: si no informa el dato (null)
        // no se avisa nada.
        SinHorario = avisarSinHorario && doctor.TieneHorario == false
    };

    /// <summary>
    /// Opciones del select, desde GET /doctores/especialidades (ya vienen sin
    /// repetir y ordenadas por la API).
    /// </summary>
    private static IReadOnlyList<SelectListItem> OpcionesDeEspecialidad(
        IEnumerable<string> especialidades, string? seleccionada) =>
        especialidades
            .Select(e => new SelectListItem(e, e, string.Equals(e, seleccionada, StringComparison.OrdinalIgnoreCase)))
            .ToList();
}
