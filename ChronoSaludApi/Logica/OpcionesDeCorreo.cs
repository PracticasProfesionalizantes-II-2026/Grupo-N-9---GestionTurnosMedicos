using MailKit.Security;

namespace ChronoSaludApi.Logica;

/// <summary>
/// La sección "Correo" de la configuración. En el repo va vacía: el servidor,
/// el usuario y sobre todo la contraseña se cargan en Azure (Correo__Smtp__Host,
/// Correo__Smtp__Contrasena, …) o en los user-secrets en local. Ver README.
/// </summary>
public class OpcionesDeCorreo
{
    /// <summary>La dirección que aparece como remitente. Si falta, se usa el usuario del SMTP.</summary>
    public string? Remitente { get; set; }

    public string NombreRemitente { get; set; } = "ChronoSalud";

    /// <summary>
    /// Sin servidor SMTP, fuera de desarrollo los correos no se mandan ni se
    /// escriben en el log (llevan enlaces para cambiar contraseñas). Con esto
    /// en true se escriben en el log igual: solo para probar.
    /// </summary>
    public bool MostrarEnLog { get; set; }

    public OpcionesDeSmtp Smtp { get; set; } = new();

    /// <summary>El remitente que se usa de verdad: el configurado o, si no, el usuario del SMTP.</summary>
    public string? RemitenteEfectivo =>
        string.IsNullOrWhiteSpace(Remitente) ? Smtp.Usuario : Remitente.Trim();
}

public class OpcionesDeSmtp
{
    public string? Host { get; set; }

    /// <summary>587 con StartTls es lo que usan Brevo, Gmail, Outlook y SendGrid.</summary>
    public int Puerto { get; set; } = 587;

    public string? Usuario { get; set; }

    public string? Contrasena { get; set; }

    /// <summary>"StartTls" (587), "SslOnConnect" (465), "Auto" o "None" (solo para un servidor de prueba local).</summary>
    public string Seguridad { get; set; } = "StartTls";

    public bool HayServidor => !string.IsNullOrWhiteSpace(Host);

    public SecureSocketOptions OpcionDeSeguridad => Seguridad?.Trim().ToLowerInvariant() switch
    {
        "sslonconnect" or "ssl" => SecureSocketOptions.SslOnConnect,
        "auto"                  => SecureSocketOptions.Auto,
        "none" or "ninguna"     => SecureSocketOptions.None,
        _                       => SecureSocketOptions.StartTls,
    };
}
