using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Models.ViewModels;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class PacientesController : ControladorBase
{
    private const int Limite = 100;

    private const string TituloSinPermiso = "No podés ver los pacientes con tu rol";
    private const string MotivoSinPermiso =
        "La API reserva el listado de pacientes a los roles doctor y administrador, " +
        "y el detalle incluye datos clínicos.";

    private const string TituloSinPermisoAlta = "No podés dar de alta pacientes con tu rol";
    private const string MotivoSinPermisoAlta =
        "El alta de pacientes está reservada al rol administrador.";

    private readonly PacienteService _pacientes;
    private readonly CoberturaService _coberturas;
    private readonly TurnoService _turnos;
    private readonly AuthService _auth;

    public PacientesController(
        PacienteService pacientes,
        CoberturaService coberturas,
        TurnoService turnos,
        AuthService auth)
    {
        _pacientes = pacientes;
        _coberturas = coberturas;
        _turnos = turnos;
        _auth = auth;
    }

    public async Task<IActionResult> Index(string? nombre)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedeVerPacientes)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var busqueda = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();

        try
        {
            var pagina = await _pacientes.BuscarAsync(busqueda, Limite);

            return View(new PacientesIndexViewModel
            {
                Nombre = busqueda,
                Total = pagina.Total,
                Pacientes = pagina.Pacientes
                    .Select(p => new PacienteFilaViewModel
                    {
                        IdPaciente = p.IdPaciente,
                        Nombre = p.Nombre,
                        Apellido = p.Apellido
                    })
                    .ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new PacientesIndexViewModel { Nombre = busqueda, Error = error.Message });
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        // Mismo criterio que el listado: acá se ven alergias y condiciones.
        if (!_auth.PuedeVerPacientes)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        try
        {
            var paciente = await _pacientes.ObtenerPorIdAsync(id);

            if (paciente is null)
                return NoEncontrado(id, "Paciente no encontrado", "paciente");

            // Coberturas y turnos son datos "extra" de la ficha: si cualquiera de
            // los dos falla, se muestra igual la ficha con lo que sí llegó, en
            // vez de tirar toda la página abajo por un servicio secundario.
            var coberturas = await ObtenerCoberturasSinRomperAsync(id);
            var ultimoTurno = await ObtenerUltimoTurnoSinRomperAsync(id);

            return View(new PacienteDetalleViewModel
            {
                IdPaciente = paciente.IdPaciente,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                FechaNacimiento = paciente.FechaNacimiento,
                Sexo = paciente.Sexo,
                GrupoSanguineo = paciente.GrupoSanguineo,
                Alergias = paciente.Alergias,
                Condiciones = paciente.Condiciones,
                Coberturas = coberturas,
                UltimoTurno = ultimoTurno
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new PacienteDetalleViewModel { IdPaciente = id, Error = error.Message });
        }
    }

    [HttpGet]
    public IActionResult Crear()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermisoAlta, MotivoSinPermisoAlta);

        return View(new PacienteCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PacienteCreateViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermisoAlta, MotivoSinPermisoAlta);

        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            // La API crea la fila en Pacientes sola al registrar el usuario.
            // TODO: enviar documento, fecha de nacimiento, género, nacionalidad,
            // estado civil, grupo sanguíneo, obra social, dirección, contacto de
            // emergencia y alergias cuando la API los acepte en el alta. La foto
            // tampoco se procesa todavía: solo se recibe el campo.
            await _auth.RegistrarComoPacienteAsync(
                modelo.Nombre, modelo.Apellido, modelo.Email, modelo.Contrasena, modelo.Telefono);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(modelo);
        }

        TempData["Exito"] = $"Paciente \"{modelo.Nombre} {modelo.Apellido}\" dado de alta correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<CoberturaFilaViewModel>> ObtenerCoberturasSinRomperAsync(int idPaciente)
    {
        try
        {
            var coberturas = await _coberturas.ObtenerDePacienteAsync(idPaciente);
            return coberturas
                .Select(c => new CoberturaFilaViewModel
                {
                    NombreCobertura = c.NombreCobertura,
                    Plan = c.Plan,
                    IdAfiliado = c.IdAfiliado
                })
                .ToList();
        }
        catch (ApiException)
        {
            return Array.Empty<CoberturaFilaViewModel>();
        }
    }

    private async Task<DateTime?> ObtenerUltimoTurnoSinRomperAsync(int idPaciente)
    {
        try
        {
            var pagina = await _turnos.ObtenerAsync(pacienteId: idPaciente, limite: Limite);
            return pagina.Turnos.Count == 0
                ? null
                : pagina.Turnos.Max(t => t.FechaInicio);
        }
        catch (ApiException)
        {
            return null;
        }
    }
}
