using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class AdminController : ControladorBase
{
    private const string TituloSinPermiso = "Esta sección es solo para administradores";
    private const string MotivoSinPermiso =
        "El hub de Cuentas (alta asistida de paciente, doctor y administrador) es de uso exclusivo " +
        "del rol administrador. POST /doctores en particular ya lo reserva la propia API a ese rol.";

    private readonly AuthService _auth;
    private readonly DoctorService _doctores;

    public AdminController(AuthService auth, DoctorService doctores)
    {
        _auth = auth;
        _doctores = doctores;
    }

    public IActionResult Index()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        return View();
    }

    [HttpGet]
    public IActionResult NuevoDoctor()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(NuevoDoctor)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        return View(new NuevoDoctorViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoDoctor(NuevoDoctorViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(NuevoDoctor)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        if (!ModelState.IsValid)
            return View(modelo);

        int idUsuario;
        try
        {
            // Paso 1: crear el Usuario con rol "doctor". Si esto falla (típicamente
            // 409 por email duplicado) no quedó nada creado.
            idUsuario = await _auth.RegistrarComoDoctorAsync(
                modelo.Nombre, modelo.Apellido, modelo.Email, modelo.Contrasena, modelo.Telefono);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(modelo);
        }

        try
        {
            // Paso 2: completar la fila de Doctores sobre ese IdUsuario.
            await _doctores.CrearAsync(idUsuario, modelo.Especialidad, modelo.Matricula, modelo.Consultorio);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // El Usuario ya existe en la API (no hay transacción entre los dos
            // POST): no tiene sentido que el admin reintente este formulario
            // completo, porque el paso 1 va a fallar por "email ya registrado".
            // Le dejamos el IdUsuario para que cierre el alta desde CompletarDoctor.
            modelo.IdUsuarioCreado = idUsuario;
            ModelState.AddModelError(
                string.Empty,
                $"Se creó el usuario \"{modelo.Email}\" (Id {idUsuario}) pero no se pudo completar el perfil de doctor: {error.Message}");
            return View(modelo);
        }

        TempData["Exito"] = $"Doctor \"{modelo.Nombre} {modelo.Apellido}\" dado de alta correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult CompletarDoctor(int? idUsuario)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(CompletarDoctor)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        return View(new CompletarDoctorViewModel { IdUsuario = idUsuario });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletarDoctor(CompletarDoctorViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(CompletarDoctor)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await _doctores.CrearAsync(modelo.IdUsuario!.Value, modelo.Especialidad, modelo.Matricula, modelo.Consultorio);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(modelo);
        }

        TempData["Exito"] = $"Perfil de doctor completado para el usuario Id {modelo.IdUsuario}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult NuevoPaciente()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(NuevoPaciente)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        return View(new NuevoPacienteViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoPaciente(NuevoPacienteViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(NuevoPaciente)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
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

    [HttpGet]
    public IActionResult NuevoAdministrador()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(NuevoAdministrador)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        return View(new NuevoAdministradorViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoAdministrador(NuevoAdministradorViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(NuevoAdministrador)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await _auth.RegistrarComoAdministradorAsync(
                modelo.Nombre, modelo.Apellido, modelo.Email, modelo.Contrasena, modelo.Telefono);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return View(modelo);
        }

        TempData["Exito"] = $"Administrador \"{modelo.Nombre} {modelo.Apellido}\" dado de alta correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
