using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class PacientesController : ControladorBase
{
    private const int Limite = 100;

    private const string TituloSinPermiso = "No podés ver los pacientes con tu rol";
    private const string MotivoSinPermiso =
        "La API reserva el listado de pacientes a los roles doctor y administrador, " +
        "y el detalle incluye datos clínicos.";

    private readonly PacienteService _pacientes;
    private readonly AuthService _auth;

    public PacientesController(PacienteService pacientes, AuthService auth)
    {
        _pacientes = pacientes;
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

            return View(new PacienteDetalleViewModel
            {
                IdPaciente = paciente.IdPaciente,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                FechaNacimiento = paciente.FechaNacimiento,
                Sexo = paciente.Sexo,
                GrupoSanguineo = paciente.GrupoSanguineo,
                Alergias = paciente.Alergias,
                Condiciones = paciente.Condiciones
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new PacienteDetalleViewModel { IdPaciente = id, Error = error.Message });
        }
    }
}
