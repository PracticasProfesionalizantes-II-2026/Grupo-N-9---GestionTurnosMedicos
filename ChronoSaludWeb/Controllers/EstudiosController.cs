using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

/// <summary>
/// Estudios médicos: el paciente ve los suyos y sus resultados; el doctor los
/// pide desde un turno; doctor y administración cargan el resultado.
/// </summary>
public class EstudiosController : ControladorBase
{
    private const string AvisoSinPerfil =
        "Tu usuario no tiene un perfil de paciente asociado, así que no podemos " +
        "mostrar tus estudios. Pedile a la administración que lo cree.";

    private const string TituloSinPermisoPedir = "Los estudios los pide el doctor";
    private const string MotivoSinPermisoPedir =
        "Un estudio se pide desde un turno de la agenda del doctor, así queda registrado quién lo pidió.";

    private const string TituloSinPermisoResultado = "No podés cargar resultados con tu rol";
    private const string MotivoSinPermisoResultado =
        "El resultado de un estudio lo cargan el doctor o la administración.";

    private readonly EstudioService _estudios;
    private readonly PacienteService _pacientes;
    private readonly TurnoService _turnos;
    private readonly AuthService _auth;
    private readonly PerfilService _perfil;

    public EstudiosController(
        EstudioService estudios,
        PacienteService pacientes,
        TurnoService turnos,
        AuthService auth,
        PerfilService perfil)
    {
        _estudios = estudios;
        _pacientes = pacientes;
        _turnos = turnos;
        _auth = auth;
        _perfil = perfil;
    }

    public async Task<IActionResult> Index(int? paciente, string? buscar)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index), new { paciente }));

        var esPaciente = _auth.SesionActual?.Rol == "paciente";

        try
        {
            var (idPaciente, aviso) = await ResolverPacienteAsync(paciente);
            if (aviso is not null)
                return View(new EstudiosIndexViewModel { Aviso = aviso, EsPaciente = esPaciente });

            // Doctor y administrador eligen al paciente con el buscador.
            ElegirPacienteViewModel? elegir = null;
            if (_auth.PuedeElegirPaciente)
            {
                var seleccion = await _pacientes.ElegirAsync(idPaciente, buscar);
                elegir = ElegirPacienteViewModel.Desde(seleccion, "Estudios", nameof(Index), buscar);
                idPaciente = seleccion.Elegido?.IdPaciente;
            }

            if (idPaciente is null)
                return View(new EstudiosIndexViewModel { ElegirPaciente = elegir });

            var estudios = await _estudios.ObtenerDePacienteAsync(idPaciente.Value);

            var filas = new List<EstudioFilaViewModel>();
            foreach (var estudio in estudios)
                filas.Add(EstudioFilaViewModel.Desde(estudio));

            return View(new EstudiosIndexViewModel
            {
                IdPaciente = idPaciente,
                NombrePaciente = elegir?.Elegido is { } elegido ? ElegirPacienteViewModel.NombreDe(elegido) : null,
                ElegirPaciente = elegir,
                PuedeCargarResultados = _auth.PuedeCargarResultados,
                EsPaciente = esPaciente,
                Estudios = filas
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new EstudiosIndexViewModel { Error = error.Message, EsPaciente = esPaciente });
        }
    }

    /// <summary>Pedir un estudio. Siempre desde un turno: así se sabe quién lo pidió.</summary>
    [HttpGet]
    public async Task<IActionResult> Crear(int turno)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear), new { turno }));

        if (!_auth.PuedePedirEstudios)
            return SinPermiso(TituloSinPermisoPedir, MotivoSinPermisoPedir);

        try
        {
            var modelo = new EstudioCrearViewModel { IdTurno = turno };
            var error = await CargarTurnoAsync(modelo);
            if (error is not null)
                return error;

            return View(modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = turno });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EstudioCrearViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedePedirEstudios)
            return SinPermiso(TituloSinPermisoPedir, MotivoSinPermisoPedir);

        if (modelo.Tipo is not null && !EstudioCrearViewModel.EsTipoValido(modelo.Tipo))
            ModelState.AddModelError(nameof(modelo.Tipo), "Elegí uno de los tipos de la lista.");

        try
        {
            // El paciente sale del turno, nunca del formulario.
            var error = await CargarTurnoAsync(modelo);
            if (error is not null)
                return error;

            if (!ModelState.IsValid)
                return View(modelo);

            await _estudios.CrearAsync(new EstudioNuevo(
                modelo.Turno!.IdPaciente,
                modelo.Turno.IdTurno,
                modelo.Tipo!,
                modelo.Descripcion!.Trim()));

            TempData["Exito"] = $"Estudio pedido: «{modelo.Descripcion.Trim()}». Cuando se cargue el resultado, el paciente va a recibir un aviso.";
            return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = modelo.Turno.IdTurno });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(modelo);
        }
    }

    /// <summary>Cargar (o corregir) el resultado de un estudio.</summary>
    [HttpGet]
    public async Task<IActionResult> Resultado(int id, int paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Resultado), new { id, paciente }));

        if (!_auth.PuedeCargarResultados)
            return SinPermiso(TituloSinPermisoResultado, MotivoSinPermisoResultado);

        try
        {
            var estudio = await _estudios.ObtenerPorIdAsync(id);
            if (estudio is null)
                return NoEncontrado(id, "Estudio no encontrado", "estudio");

            var fila = EstudioFilaViewModel.Desde(estudio);
            return View(new EstudioResultadoViewModel
            {
                IdEstudio = id,
                IdPaciente = paciente,
                Resultado = estudio.Resultado,
                ArchivoUrl = estudio.ArchivoUrl,
                Estudio = fila,
                NombrePaciente = await NombreDePacienteAsync(paciente)
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Index), new { paciente });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resultado(EstudioResultadoViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedeCargarResultados)
            return SinPermiso(TituloSinPermisoResultado, MotivoSinPermisoResultado);

        // El enlace es opcional; si viene, tiene que ser https.
        var enlace = string.IsNullOrWhiteSpace(modelo.ArchivoUrl) ? null : modelo.ArchivoUrl.Trim();
        if (enlace is not null && !EstudioResultadoViewModel.EsEnlaceValido(enlace))
            ModelState.AddModelError(nameof(modelo.ArchivoUrl), "Pegá la dirección completa del archivo, empezando con https://.");

        try
        {
            var estudio = await _estudios.ObtenerPorIdAsync(modelo.IdEstudio);
            if (estudio is null)
                return NoEncontrado(modelo.IdEstudio, "Estudio no encontrado", "estudio");

            if (!ModelState.IsValid)
            {
                modelo.Estudio = EstudioFilaViewModel.Desde(estudio);
                modelo.NombrePaciente = await NombreDePacienteAsync(modelo.IdPaciente);
                return View(modelo);
            }

            await _estudios.CargarResultadoAsync(modelo.IdEstudio, new EstudioResultado(modelo.Resultado!.Trim(), enlace));

            TempData["Exito"] = "Resultado guardado. Le avisamos al paciente que ya lo puede ver.";
            return RedirectToAction(nameof(Index), new { paciente = modelo.IdPaciente });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(modelo);
        }
    }

    /// <summary>
    /// Completa el turno y el paciente del formulario de pedido. Devuelve la
    /// respuesta a dar si el turno no sirve, o null si está todo bien.
    /// </summary>
    private async Task<IActionResult?> CargarTurnoAsync(EstudioCrearViewModel modelo)
    {
        if (modelo.IdTurno is null)
            return NoEncontrado(0, "Turno no encontrado", "turno");

        // Para un doctor, la API solo devuelve los turnos de su agenda.
        var turno = await _turnos.ObtenerPorIdAsync(modelo.IdTurno.Value);
        if (turno is null)
            return NoEncontrado(modelo.IdTurno.Value, "Turno no encontrado", "turno");

        var atendido = TurnoAtendidoViewModel.Desde(turno);
        if (!atendido.SePuedeAtender)
        {
            TempData["Error"] = $"El turno #{atendido.IdTurno} está {atendido.Estado}: no se le puede pedir un estudio.";
            return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = atendido.IdTurno });
        }

        modelo.Turno = atendido;
        modelo.NombrePaciente = await NombreDePacienteAsync(atendido.IdPaciente);
        return null;
    }

    private async Task<(int? idPaciente, string? aviso)> ResolverPacienteAsync(int? pedido)
    {
        if (_auth.PuedeElegirPaciente)
            return (pedido is > 0 ? pedido : null, null);

        if (_auth.SesionActual?.Rol == "paciente")
        {
            var propio = await _perfil.IdPerfilAsync(esDoctor: false);
            return propio is null ? (null, AvisoSinPerfil) : (propio, null);
        }

        return (null, "No hay estudios para mostrar con tu rol.");
    }

    private async Task<string?> NombreDePacienteAsync(int idPaciente)
    {
        // Solo doctor y administrador pueden pedir la ficha del paciente.
        if (!_auth.PuedeVerPacientes)
            return null;

        var paciente = await _pacientes.ObtenerPorIdAsync(idPaciente);
        return paciente is null ? null : $"{paciente.Nombre} {paciente.Apellido}".Trim();
    }
}
