using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Filters;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

// Login, registro y salir: por definición se usan sin sesión.
[PermiteSinSesion]
public class CuentaController : Controller
{
    private readonly AuthService _auth;
    private readonly LimiteLogin _limite;
    private readonly ILogger<CuentaController> _logger;

    public CuentaController(AuthService auth, LimiteLogin limite, ILogger<CuentaController> logger)
    {
        _auth = auth;
        _limite = limite;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya hay sesión no tiene sentido mostrar el formulario.
        if (_auth.HaySesion)
            return RedirigirA(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        // Tope de intentos por IP. Va después de validar el formulario (uno
        // vacío no gasta cupo) y antes de la API (uno bloqueado no llega).
        var ip = HttpContext.Connection.RemoteIpAddress;
        if (!_limite.PermiteIntento(ip))
        {
            // Solo la IP: nada de lo que escribió el usuario va al log.
            _logger.LogWarning("Login bloqueado por límite de intentos desde {Ip}", ip);

            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ModelState.AddModelError(string.Empty, "Demasiados intentos. Esperá unos minutos y volvé a probar.");
            return View(modelo);
        }

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
