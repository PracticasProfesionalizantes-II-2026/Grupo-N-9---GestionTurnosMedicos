namespace ChronoSaludWeb.Services;

/// <summary>
/// Respuesta de POST /usuarios/login. Espeja LoginResponseDto de la API.
/// </summary>
public record LoginRespuesta(string Token, string Rol, int IdUsuario, string Nombre);

/// <summary>
/// Login / logout contra la API. El token se guarda en la sesión del servidor,
/// así que el navegador solo se lleva la cookie de sesión.
/// </summary>
public class AuthService
{
    private readonly ApiClient _api;
    private readonly IHttpContextAccessor _contexto;

    public AuthService(ApiClient api, IHttpContextAccessor contexto)
    {
        _api = api;
        _contexto = contexto;
    }

    public SesionUsuario? SesionActual => _contexto.HttpContext?.Session.ObtenerSesion();

    public bool HaySesion => SesionActual is not null;

    /// <summary>
    /// Autentica contra la API y deja la sesión abierta.
    /// Tira <see cref="ApiException"/> si las credenciales no sirven o la API no responde.
    /// </summary>
    public async Task<SesionUsuario> LoginAsync(string email, string contrasena)
    {
        LoginRespuesta? respuesta;
        try
        {
            // anonimo: true porque todavía no hay token y porque un 401 acá
            // significa "credenciales incorrectas", no "sesión vencida".
            respuesta = await _api.PostAsync<LoginRespuesta>(
                "/usuarios/login",
                new { email, contrasena },
                anonimo: true);
        }
        catch (ApiException ex) when (ex.Status == StatusCodes.Status401Unauthorized)
        {
            // La API responde 401 sin cuerpo y a propósito no aclara si falló
            // el email o la contraseña.
            throw new ApiException("Email o contraseña incorrectos.", ex.Status);
        }

        if (respuesta is null || string.IsNullOrEmpty(respuesta.Token))
            throw new ApiException("La API no devolvió un token de sesión.", 0);

        var sesion = new SesionUsuario(
            respuesta.Token, respuesta.Rol, respuesta.IdUsuario, respuesta.Nombre);

        _contexto.HttpContext!.Session.GuardarSesion(sesion);
        return sesion;
    }

    public void Logout() => _contexto.HttpContext?.Session.CerrarSesion();
}
