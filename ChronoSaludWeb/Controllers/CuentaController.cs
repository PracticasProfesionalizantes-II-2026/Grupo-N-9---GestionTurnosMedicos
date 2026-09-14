using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class CuentaController : Controller
{
    private readonly AuthService _auth;
    private readonly string? _apiBaseUrl;

    public CuentaController(AuthService auth, IConfiguration configuration)
    {
        _auth = auth;
        _apiBaseUrl = configuration["Api:BaseUrl"];
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya hay sesión no tiene sentido mostrar el formulario.
        if (_auth.HaySesion)
            return RedirigirA(returnUrl);

        ViewData["ApiBaseUrl"] = _apiBaseUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        ViewData["ApiBaseUrl"] = _apiBaseUrl;

        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await _auth.LoginAsync(modelo.Email, modelo.Contrasena);
        }
        catch (ApiException ex)
        {
            // Credenciales incorrectas, API caída, etc. El mensaje ya viene armado.
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        return RedirigirA(modelo.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Registro()
    {
        // Igual que Login: con sesión abierta no tiene sentido este formulario.
        if (_auth.HaySesion)
            return RedirigirA(null);

        return View(new RegistroViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await _auth.RegistrarPacienteAsync(
                modelo.Nombre, modelo.Apellido, modelo.Email, modelo.Contrasena, modelo.Telefono);
        }
        catch (ApiException ex)
        {
            // Email ya registrado (409), datos inválidos (400), API caída, etc.
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        // El registro ya deja la sesión abierta: de acá se va a completar la
        // ficha clínica, que queda vacía apenas se crea el perfil de paciente.
        return RedirectToAction("CompletarPaciente", "MiPerfil");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _auth.Logout();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Vuelve a donde el usuario quería ir. Se valida que la URL sea local para
    /// que nadie pueda usar ?returnUrl= para mandarlo a otro sitio.
    /// </summary>
    private IActionResult RedirigirA(string? returnUrl)
        => Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction("Index", "Home");
}
