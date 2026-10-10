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
        // ficha, que queda vacía apenas se crea el perfil de paciente.
        TempData["Exito"] = "Tu cuenta está lista. Si querés, completá ahora tu ficha; también podés hacerlo más tarde desde Mi perfil.";
        return RedirectToAction("Editar", "MiPerfil");
    }

    /// <summary>"¿Olvidaste tu contraseña?": se pide el email de la cuenta.</summary>
    [HttpGet]
    public IActionResult Recuperar()
    {
        if (_auth.HaySesion)
            return RedirigirA(null);

        return View(new RecuperarViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recuperar(RecuperarViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        // El mismo tope por IP que el login: frena a quien pide enlaces sin parar.
        var ip = HttpContext.Connection.RemoteIpAddress;
        if (!_limite.PermiteIntento(ip))
        {
            _logger.LogWarning("Pedido de recuperación bloqueado por límite de intentos desde {Ip}", ip);

            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ModelState.AddModelError(string.Empty, "Demasiados intentos. Esperá unos minutos y volvé a probar.");
            return View(modelo);
        }

        try
        {
            await _auth.PedirRecuperacionAsync(modelo.Email);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        // El mismo mensaje exista o no la cuenta: así no se puede averiguar
        // qué emails están registrados.
        modelo.Enviado = true;
        return View(modelo);
    }

    /// <summary>La pantalla del enlace del email: se elige la contraseña nueva.</summary>
    [HttpGet]
    public IActionResult Restablecer(string? token)
    {
        return View(new RestablecerViewModel { Token = token ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restablecer(RestablecerViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await _auth.RestablecerContrasenaAsync(modelo.Token, modelo.Contrasena);
        }
        catch (ApiException ex)
        {
            // Enlace vencido o ya usado (400), API caída, etc.
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }

        // Si había una sesión abierta en este navegador, se cierra: la
        // contraseña cambió y conviene entrar de nuevo.
        _auth.Logout();
        TempData["Exito"] = "Listo, ya podés entrar con tu contraseña nueva.";
        return RedirectToAction(nameof(Login));
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
