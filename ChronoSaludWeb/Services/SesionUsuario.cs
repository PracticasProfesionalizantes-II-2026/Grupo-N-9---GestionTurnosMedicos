using System.Text.Json;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Lo que devuelve el login de la API y guardamos del lado del servidor.
/// Equivale al objeto que el front viejo dejaba en localStorage (js/sesion.js),
/// pero acá vive en la sesión de ASP.NET: el token nunca llega al navegador.
/// </summary>
public record SesionUsuario(string Token, string Rol, int IdUsuario, string Nombre);

/// <summary>
/// Acceso a la sesión del usuario. Está como extensiones de ISession para que
/// tanto el ApiClient (que lee el token) como el AuthService (que lo escribe)
/// usen la misma clave sin depender uno del otro.
/// </summary>
public static class SesionExtensiones
{
    private const string Clave = "chronosalud.sesion";

    public static void GuardarSesion(this ISession sesion, SesionUsuario datos)
        => sesion.SetString(Clave, JsonSerializer.Serialize(datos));

    public static SesionUsuario? ObtenerSesion(this ISession sesion)
    {
        var json = sesion.GetString(Clave);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<SesionUsuario>(json);
        }
        catch (JsonException)
        {
            // Sesión vieja o corrupta: la tratamos como si no hubiera sesión.
            return null;
        }
    }

    public static void CerrarSesion(this ISession sesion) => sesion.Remove(Clave);

    public static bool TieneRol(this ISession sesion, params string[] roles)
    {
        var actual = sesion.ObtenerSesion();
        return actual != null && roles.Contains(actual.Rol);
    }
}
