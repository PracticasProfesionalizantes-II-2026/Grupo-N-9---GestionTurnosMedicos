using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    private readonly AuthService _auth;
    private readonly PerfilService _perfil;

    public HistorialController(
        HistorialService historial,
        PacienteService pacientes,
        AuthService auth,
        PerfilService perfil)
    {
        _historial = historial;
        _pacientes = pacientes;
        _auth = auth;
        _perfil = perfil;
    }

    public async Task<IActionResult> Index(int? paciente, DateTime? desde, DateTime? hasta)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index), new { paciente }));

        var filtros = new HistorialFiltroViewModel { Desde = desde, Hasta = hasta };

        try
        {
            var (idPaciente, aviso) = await ResolverPacienteAsync(paciente);

            if (aviso is not null)
                return View(new HistorialIndexViewModel { Aviso = aviso, Filtros = filtros });

            // Doctor y administrador todavía no eligieron de quién.
            if (idPaciente is null)
            {
                return View(new HistorialIndexViewModel
                {
                    Filtros = filtros,
                    PuedeElegirPaciente = true,
                    Pacientes = await OpcionesDePacienteAsync(null),
                    PuedeEscribir = _auth.PuedeEscribirHistorial
                });
            }

            var entradas = await _historial.ObtenerDePacienteAsync(idPaciente.Value, desde, hasta);

            return View(new HistorialIndexViewModel
            {
                IdPaciente = idPaciente,
                NombrePaciente = await NombreDePacienteAsync(idPaciente.Value),
                Filtros = filtros,
                PuedeElegirPaciente = _auth.PuedeElegirPaciente,
                Pacientes = _auth.PuedeElegirPaciente
                    ? await OpcionesDePacienteAsync(idPaciente)
                    : Array.Empty<SelectListItem>(),
                PuedeEscribir = _auth.PuedeEscribirHistorial,
                Entradas = entradas
                    .Select(Mapear)
                    .OrderByDescending(e => e.Fecha)
                    .ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new HistorialIndexViewModel { Filtros = filtros, Error = error.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Crear(int? paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear), new { paciente }));

        if (!_auth.PuedeEscribirHistorial)
            return SinPermisoDeEscritura();

        var modelo = new EntradaHistorialCrearViewModel
        {
            IdPaciente = paciente,
            Fecha = DateTime.Today
        };

        await CargarListaAsync(modelo);
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

        try
        {
            await _historial.CrearAsync(modelo.IdPaciente!.Value, new EntradaHistorialNueva(
                modelo.Fecha!.Value,
                modelo.Descripcion.Trim(),
                modelo.Diagnostico.Trim(),
                // La API acepta vincular la entrada a un turno; todavía no lo pedimos.
                IdTurno: null));

            TempData["Exito"] = "Entrada agregada a la historia clínica.";
            return RedirectToAction(nameof(Index), new { paciente = modelo.IdPaciente });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            await CargarListaAsync(modelo);
            return View(modelo);
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

    private async Task<IReadOnlyList<SelectListItem>> OpcionesDePacienteAsync(int? seleccionado)
    {
        var pacientes = await _pacientes.ObtenerTodosAsync();
        return pacientes
            .Select(p => new SelectListItem(
                $"{p.Nombre} {p.Apellido}".Trim(),
                p.IdPaciente.ToString(),
                p.IdPaciente == seleccionado))
            .ToList();
    }

    private async Task CargarListaAsync(EntradaHistorialCrearViewModel modelo)
    {
        try
        {
            modelo.Pacientes = await OpcionesDePacienteAsync(modelo.IdPaciente);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // La lista queda vacía, pero no se pierde lo que el doctor escribió.
            ModelState.AddModelError(string.Empty, $"No se pudo cargar la lista de pacientes: {error.Message}");
        }
    }

    private IActionResult SinPermisoDeEscritura() => SinPermiso(
        "No podés escribir en la historia clínica con tu rol",
        "La API reserva la carga de entradas del historial al rol doctor.");

    private static EntradaHistorialViewModel Mapear(EntradaHistorial entrada) => new()
    {
        IdHistorial = entrada.IdHistorial,
        Fecha = entrada.Fecha,
        Descripcion = entrada.Descripcion,
        Diagnostico = entrada.Diagnostico,
        IdTurno = entrada.IdTurno
    };
}
