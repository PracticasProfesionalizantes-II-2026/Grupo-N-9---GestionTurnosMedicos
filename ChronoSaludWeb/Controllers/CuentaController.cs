using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class CuentaController : Controller
{
    private readonly AuthService _auth;

    public CuentaController(AuthService auth) => _auth = auth;

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
