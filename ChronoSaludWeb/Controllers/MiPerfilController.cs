using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class MiPerfilController : ControladorBase
{
    private const string TituloSinPermiso = "Esta pantalla es solo para pacientes";
    private const string MotivoSinPermiso =
        "La ficha clínica de PUT /pacientes/{id} es propia de cada paciente, así " +
        "que solo tiene sentido completarla con una sesión de rol paciente.";

    private readonly PacienteService _pacientes;
    private readonly PerfilService _perfil;
    private readonly AuthService _auth;

    public MiPerfilController(PacienteService pacientes, PerfilService perfil, AuthService auth)
    {
        _pacientes = pacientes;
        _perfil = perfil;
        _auth = auth;
    }

    public async Task<IActionResult> CompletarPaciente()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(CompletarPaciente)));

        if (_auth.SesionActual!.Rol != "paciente")
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var idPaciente = await _perfil.IdPerfilAsync(esDoctor: false);
        if (idPaciente is null)
        {
            // Caso residual: un Usuario rol paciente sin fila en Pacientes,
            // algo que ya no puede pasar con el registro público (crea la fila
            // siempre) pero sí con datos cargados por otra vía.
            TempData["Error"] =
                "Tu usuario todavía no tiene un perfil de paciente asociado. Pedile a la administración que lo cree.";
            return RedirectToAction("Index", "Home");
        }

        var perfil = await _pacientes.ObtenerMiPerfilAsync();

        return View(new CompletarPerfilPacienteViewModel
        {
            FechaNacimiento = perfil?.FechaNacimiento,
            Sexo = perfil?.Sexo,
            GrupoSanguineo = perfil?.GrupoSanguineo,
            Alergias = perfil?.Alergias,
            Condiciones = perfil?.Condiciones
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletarPaciente(CompletarPerfilPacienteViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(CompletarPaciente)));

        if (_auth.SesionActual!.Rol != "paciente")
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        if (!ModelState.IsValid)
            return View(modelo);

        var idPaciente = await _perfil.IdPerfilAsync(esDoctor: false);
        if (idPaciente is null)
        {
            TempData["Error"] =
                "Tu usuario todavía no tiene un perfil de paciente asociado. Pedile a la administración que lo cree.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            await _pacientes.ActualizarAsync(
                idPaciente.Value,
                modelo.FechaNacimiento,
                modelo.Sexo,
                modelo.GrupoSanguineo,
                modelo.Alergias,
                modelo.Condiciones);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            modelo.Error = error.Message;
            return View(modelo);
        }

        return RedirectToAction("Index", "Home");
    }
}
