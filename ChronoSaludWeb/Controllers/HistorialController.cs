using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class HistorialController : ControladorBase
{
    private const string AvisoSinPerfil =
        "Tu usuario no tiene un perfil de paciente asociado, así que no podemos " +
        "mostrar tu historia clínica. Pedile a la administración que lo cree.";

    private readonly HistorialService _historial;
    private readonly PacienteService _pacientes;
    private readonly TurnoService _turnos;
    private readonly AuthService _auth;
    private readonly PerfilService _perfil;

    public HistorialController(
        HistorialService historial,
        PacienteService pacientes,
        TurnoService turnos,
        AuthService auth,
        PerfilService perfil)
    {
        _historial = historial;
        _pacientes = pacientes;
        _turnos = turnos;
        _auth = auth;
        _perfil = perfil;
    }

    public async Task<IActionResult> Index(int? paciente, DateTime? desde, DateTime? hasta, string? buscar)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index), new { paciente }));

        var filtros = new HistorialFiltroViewModel { Desde = desde, Hasta = hasta };

        try
        {
            var (idPaciente, aviso) = await ResolverPacienteAsync(paciente);

            if (aviso is not null)
                return View(new HistorialIndexViewModel { Aviso = aviso, Filtros = filtros });

            // Doctor y administrador eligen al paciente con el buscador. Las
            // fechas del filtro siguen puestas si se cambia de paciente.
            ElegirPacienteViewModel? elegir = null;
            if (_auth.PuedeElegirPaciente)
            {
                var seleccion = await _pacientes.ElegirAsync(idPaciente, buscar);
                elegir = ElegirPacienteViewModel.Desde(seleccion, "Historial", nameof(Index), buscar, filtros.Ruta);
                idPaciente = seleccion.Elegido?.IdPaciente;
            }

            // Todavía no eligieron de quién.
            if (idPaciente is null)
            {
                return View(new HistorialIndexViewModel
                {
                    Filtros = filtros,
                    ElegirPaciente = elegir,
                    PuedeEscribir = _auth.PuedeEscribirHistorial
                });
            }

            var entradas = await _historial.ObtenerDePacienteAsync(idPaciente.Value, desde, hasta);
            var idDoctorPropio = await IdDoctorPropioAsync();

            return View(new HistorialIndexViewModel
            {
                IdPaciente = idPaciente,
                NombrePaciente = elegir?.Elegido is { } elegido
                    ? ElegirPacienteViewModel.NombreDe(elegido)
                    : await NombreDePacienteAsync(idPaciente.Value),
                Filtros = filtros,
                ElegirPaciente = elegir,
                PuedeEscribir = _auth.PuedeEscribirHistorial,
                Entradas = entradas
                    .Select(e => Mapear(e, idDoctorPropio))
                    .OrderByDescending(e => e.Fecha)
                    .ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new HistorialIndexViewModel { Filtros = filtros, Error = error.Message });
        }
    }

    /// <summary>
    /// Nueva entrada. Con <paramref name="turno"/> es la consulta de ese turno:
    /// el paciente y la fecha salen del turno y la entrada queda vinculada.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Crear(int? paciente, int? turno, string? buscar)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear), new { paciente, turno }));

        if (!_auth.PuedeEscribirHistorial)
            return SinPermisoDeEscritura();

        var modelo = new EntradaHistorialCrearViewModel
        {
            IdPaciente = paciente,
            Fecha = FechaArgentina.Hoy()
        };

        if (turno is not null)
        {
            try
            {
                var atendido = await TurnoParaAtenderAsync(turno.Value);
                if (atendido is null)
                    return NoEncontrado(turno.Value, "Turno no encontrado", "turno");

                if (!atendido.SePuedeAtender)
                {
                    TempData["Error"] = $"El turno #{atendido.IdTurno} está {atendido.Estado}: no se le puede registrar una consulta.";
                    return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = atendido.IdTurno });
                }

                modelo.IdTurno = atendido.IdTurno;
                modelo.IdPaciente = atendido.IdPaciente;
                modelo.Fecha = atendido.FechaInicio.Date;
                modelo.CompletarTurno = atendido.PuedeCompletarse;
                modelo.Turno = atendido;
            }
            catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
            {
                TempData["Error"] = error.Message;
                return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = turno });
            }
        }

        await CargarListaAsync(modelo, buscar);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EntradaHistorialCrearViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.PuedeEscribirHistorial)
            return SinPermisoDeEscritura();

        if (!ModelState.IsValid)
        {
            await CargarListaAsync(modelo);
            return View(modelo);
        }

        TurnoAtendidoViewModel? atendido = null;

        try
        {
            // Desde un turno, el paciente sale del turno y no del formulario.
            if (modelo.IdTurno is not null)
            {
                atendido = await TurnoParaAtenderAsync(modelo.IdTurno.Value);
                if (atendido is null)
                    return NoEncontrado(modelo.IdTurno.Value, "Turno no encontrado", "turno");

                modelo.IdPaciente = atendido.IdPaciente;
            }

            await _historial.CrearAsync(modelo.IdPaciente!.Value, new EntradaHistorialNueva(
                modelo.Fecha!.Value,
                modelo.Descripcion.Trim(),
                modelo.Diagnostico.Trim(),
                modelo.IdTurno));
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            await CargarListaAsync(modelo);
            return View(modelo);
        }

        if (atendido is null)
        {
            TempData["Exito"] = "Entrada agregada a la historia clínica.";
            return RedirectToAction(nameof(Index), new { paciente = modelo.IdPaciente });
        }

        // Desde un turno se vuelve al turno.
        TempData["Exito"] = "Consulta registrada en la historia clínica.";
        if (modelo.CompletarTurno)
            await CompletarTurnoAsync(atendido.IdTurno);

        return RedirectToAction(nameof(TurnosController.Detalle), "Turnos", new { id = atendido.IdTurno });
    }

    /// <summary>
    /// Editar una entrada. Solo la ve el doctor que la escribió: para
    /// cualquier otro, la entrada "no existe".
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Editar(int id, int paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar), new { id, paciente }));

        if (!_auth.PuedeEscribirHistorial)
            return SinPermisoDeEscritura();

        try
        {
            var entrada = await BuscarEntradaPropiaAsync(id, paciente);
            if (entrada is null)
                return NoEncontrado(id, "Entrada no encontrada", "registro de la historia clínica");

            var modelo = new EntradaHistorialCrearViewModel
            {
                IdHistorial = entrada.IdHistorial,
                IdPaciente = paciente,
                IdTurno = entrada.IdTurno,
                Fecha = entrada.Fecha.Date,
                Diagnostico = entrada.Diagnostico,
                Descripcion = entrada.Descripcion
            };

            await CargarListaAsync(modelo);
            return View(nameof(Crear), modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Index), new { paciente });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EntradaHistorialCrearViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedeEscribirHistorial)
            return SinPermisoDeEscritura();

        if (modelo.IdHistorial is null || modelo.IdPaciente is null)
            return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid)
        {
            await CargarListaAsync(modelo);
            return View(nameof(Crear), modelo);
        }

        try
        {
            // Se vuelve a revisar que sea suya: el formulario se puede armar a mano.
            var entrada = await BuscarEntradaPropiaAsync(modelo.IdHistorial.Value, modelo.IdPaciente.Value);
            if (entrada is null)
                return NoEncontrado(modelo.IdHistorial.Value, "Entrada no encontrada", "registro de la historia clínica");

            // El turno vinculado no cambia al editar: va el que ya tenía.
            await _historial.ActualizarAsync(modelo.IdPaciente.Value, modelo.IdHistorial.Value, new EntradaHistorialNueva(
                modelo.Fecha!.Value,
                modelo.Descripcion.Trim(),
                modelo.Diagnostico.Trim(),
                entrada.IdTurno));

            TempData["Exito"] = "Entrada actualizada.";
            return RedirectToAction(nameof(Index), new { paciente = modelo.IdPaciente });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            await CargarListaAsync(modelo);
            return View(nameof(Crear), modelo);
        }
    }

    /// <summary>
    /// Un paciente solo ve su historia: el id pedido se ignora y se usa el de su
    /// perfil. Doctor y administrador pueden ver la de cualquiera.
    /// </summary>
    private async Task<(int? idPaciente, string? aviso)> ResolverPacienteAsync(int? pedido)
    {
        if (_auth.PuedeElegirPaciente)
            return (pedido is > 0 ? pedido : null, null);

        if (_auth.SesionActual?.Rol == "paciente")
        {
            var propio = await _perfil.IdPerfilAsync(esDoctor: false);
            return propio is null ? (null, AvisoSinPerfil) : (propio, null);
        }

        return (null, "No hay historia clínica para mostrar con tu rol.");
    }

    private async Task<string?> NombreDePacienteAsync(int idPaciente)
    {
        // Solo doctor y administrador pueden pedir la ficha del paciente.
        if (!_auth.PuedeVerPacientes)
            return _auth.SesionActual?.Nombre;

        var paciente = await _pacientes.ObtenerPorIdAsync(idPaciente);
        return paciente is null ? null : $"{paciente.Nombre} {paciente.Apellido}".Trim();
    }

    /// <summary>
    /// Lo que el formulario muestra y no se postea: el paciente elegido o, si
    /// todavía no hay uno, el buscador (<paramref name="buscar"/> es lo que se
    /// escribió ahí); y el turno, si tiene.
    /// </summary>
    private async Task CargarListaAsync(EntradaHistorialCrearViewModel modelo, string? buscar = null)
    {
        try
        {
            // Desde un turno o al editar, el paciente no se puede cambiar.
            var seleccion = await _pacientes.ElegirAsync(modelo.IdPaciente, buscar);
            modelo.IdPaciente = seleccion.Elegido?.IdPaciente;
            modelo.ElegirPaciente = ElegirPacienteViewModel.Desde(
                seleccion, "Historial", nameof(Crear), buscar, puedeCambiar: modelo.PuedeCambiarPaciente);

            if (modelo.IdTurno is not null && modelo.Turno is null)
                modelo.Turno = await TurnoParaAtenderAsync(modelo.IdTurno.Value);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // Falta algún dato para mostrar, pero no se pierde lo que el doctor escribió.
            ModelState.AddModelError(string.Empty, $"No se pudieron cargar todos los datos: {error.Message}");
        }
    }

    /// <summary>
    /// El turno, si quien pregunta lo puede ver. Para un doctor, la API solo
    /// devuelve los de su agenda: uno ajeno llega como null.
    /// </summary>
    private async Task<TurnoAtendidoViewModel?> TurnoParaAtenderAsync(int idTurno)
    {
        var turno = await _turnos.ObtenerPorIdAsync(idTurno);
        return turno is null ? null : TurnoAtendidoViewModel.Desde(turno);
    }

    /// <summary>
    /// Marca el turno como completado. La consulta ya quedó guardada: si la API
    /// no lo permite (por ejemplo, el turno todavía no empezó), solo se avisa.
    /// </summary>
    private async Task CompletarTurnoAsync(int idTurno)
    {
        try
        {
            await _turnos.CambiarEstadoAsync(idTurno, "completado");
            TempData["Exito"] = $"Consulta registrada y turno #{idTurno} marcado como completado.";
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            TempData["Error"] = $"El turno #{idTurno} no se pudo marcar como completado: {error.Message}";
        }
    }

    /// <summary>
    /// Una entrada del paciente escrita por el doctor que pregunta. Null si no
    /// existe, si es de otro paciente o si la escribió otro doctor.
    /// </summary>
    private async Task<EntradaHistorial?> BuscarEntradaPropiaAsync(int id, int paciente)
    {
        var idDoctorPropio = await IdDoctorPropioAsync();
        if (idDoctorPropio is null || paciente <= 0)
            return null;

        var entrada = (await _historial.ObtenerDePacienteAsync(paciente))
            .FirstOrDefault(e => e.IdHistorial == id);

        if (entrada?.Doctor is null || entrada.Doctor.IdDoctor != idDoctorPropio)
            return null;

        return entrada;
    }

    /// <summary>El perfil de doctor de quien está logueado, o null si no es doctor.</summary>
    private async Task<int?> IdDoctorPropioAsync() =>
        _auth.EsDoctor ? await _perfil.IdPerfilAsync(esDoctor: true) : null;

    private IActionResult SinPermisoDeEscritura() => SinPermiso(
        "No podés escribir en la historia clínica con tu rol",
        "Solo los doctores pueden cargar entradas en la historia clínica.");

    private EntradaHistorialViewModel Mapear(EntradaHistorial entrada, int? idDoctorPropio)
    {
        var firma = FirmaViewModel.Desde(entrada.Doctor);
        var esSuya = idDoctorPropio is not null && firma?.IdDoctor == idDoctorPropio;

        return new EntradaHistorialViewModel
        {
            IdHistorial = entrada.IdHistorial,
            Fecha = entrada.Fecha,
            Descripcion = entrada.Descripcion,
            Diagnostico = entrada.Diagnostico,
            IdTurno = entrada.IdTurno,
            Firma = firma,
            PuedeEditar = esSuya,
            // El paciente y la administración ven el turno; un doctor, solo los de su agenda.
            EnlaceAlTurno = !_auth.EsDoctor || esSuya
        };
    }
}
