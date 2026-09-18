namespace ChronoSaludWeb.Services;

/// <summary>
/// Tema y escala tipográfica elegidos por el usuario. Los valores son los que
/// consume el CSS tal cual, sin traducción intermedia: van directo a los
/// atributos data-theme y data-scale del &lt;html&gt;. "light" y "md" no
/// matchean ninguna regla de app.css, que es justo el aspecto por defecto.
/// </summary>
public record PreferenciasVisuales(string Tema, string Escala)
{
    public static PreferenciasVisuales Defecto { get; } = new("light", "md");

    public static readonly string[] TemasValidos = ["light", "dark"];
    public static readonly string[] EscalasValidas = ["md", "lg", "xl"];
}

/// <summary>
/// Acceso a la cookie de preferencias. Está como extensiones de HttpRequest y
/// HttpResponse para que el AjustesController (que la escribe) y el layout
/// (que la lee en cada render) usen la misma clave sin depender uno del otro,
/// igual que <see cref="SesionExtensiones"/> con la sesión.
///
/// Va en una cookie del cliente y no en la sesión del servidor a propósito: es
/// una preferencia de accesibilidad, así que tiene que sobrevivir al logout y
/// al vencimiento de la sesión, y aplicarse también a quien todavía no entró.
/// </summary>
public static class PreferenciasExtensiones
{
    private const string Clave = "ChronoSalud.Preferencias";

    // Un solo valor "tema|escala" en vez de dos cookies: se leen y se escriben
    // siempre juntas, y así no puede quedar una sin la otra.
    private const char Separador = '|';

    public static PreferenciasVisuales LeerPreferencias(this HttpRequest peticion)
    {
        if (!peticion.Cookies.TryGetValue(Clave, out var valor) || string.IsNullOrEmpty(valor))
            return PreferenciasVisuales.Defecto;

        var partes = valor.Split(Separador);
        if (partes.Length != 2)
            return PreferenciasVisuales.Defecto;

        // La cookie la manda el navegador, así que su contenido es entrada del
        // usuario: cualquier valor fuera de la lista blanca cae al default en
        // lugar de terminar dentro de un atributo del <html>.
        return new PreferenciasVisuales(
            TemaValido(partes[0]),
            EscalaValida(partes[1]));
    }

    public static void GuardarPreferencias(this HttpResponse respuesta, PreferenciasVisuales preferencias)
    {
        respuesta.Cookies.Append(Clave, $"{preferencias.Tema}{Separador}{preferencias.Escala}", new CookieOptions
        {
            // Un año: es una preferencia estable, no algo de la visita actual.
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            MaxAge = TimeSpan.FromDays(365),

            // Sin esto la política de consentimiento de cookies la bloquearía y
            // los ajustes no se guardarían hasta que el usuario acepte.
            IsEssential = true,

            // No hay JavaScript en la app: la cookie la lee solo el servidor.
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    public static string TemaValido(string? valor) =>
        PreferenciasVisuales.TemasValidos.Contains(valor)
            ? valor!
            : PreferenciasVisuales.Defecto.Tema;

    public static string EscalaValida(string? valor) =>
        PreferenciasVisuales.EscalasValidas.Contains(valor)
            ? valor!
            : PreferenciasVisuales.Defecto.Escala;
}
