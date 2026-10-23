using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ChronoSaludApi.Logica;

/// <summary>
/// Manda el email de verdad por SMTP (MailKit). Sirve con cualquier proveedor
/// que dé SMTP (Brevo, Gmail, Outlook, SendGrid, Mailtrap…): cambia solo la
/// configuración (ver <see cref="OpcionesDeCorreo"/>). Cada correo va en texto
/// y en HTML (<see cref="PlantillaDeCorreo"/>).
/// </summary>
public class CorreoPorSmtp : ITransporteDeCorreo
{
    private readonly OpcionesDeCorreo _opciones;

    public CorreoPorSmtp(IOptions<OpcionesDeCorreo> opciones)
    {
        _opciones = opciones.Value;
    }

    public async Task Mandar(CorreoSaliente correo, CancellationToken cancelacion)
    {
        var mensaje = Armar(correo, _opciones);
        var smtp = _opciones.Smtp;

        using var cliente = new SmtpClient { Timeout = 30_000 };
        // Este transporte se elige solo cuando hay Host (EleccionDeTransporte).
        await cliente.ConnectAsync(smtp.Host!, smtp.Puerto, smtp.OpcionDeSeguridad, cancelacion);
        if (!string.IsNullOrEmpty(smtp.Usuario))
            await cliente.AuthenticateAsync(smtp.Usuario, smtp.Contrasena ?? string.Empty, cancelacion);
        await cliente.SendAsync(mensaje, cancelacion);
        await cliente.DisconnectAsync(true, cancelacion);
    }

    /// <summary>El mensaje MIME con su versión en texto y en HTML. Aparte para poder probarlo.</summary>
    public static MimeMessage Armar(CorreoSaliente correo, OpcionesDeCorreo opciones)
    {
        var remitente = opciones.RemitenteEfectivo
            ?? throw new InvalidOperationException("Falta el remitente del correo (Correo:Remitente o Correo:Smtp:Usuario).");

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(opciones.NombreRemitente, remitente));
        mensaje.To.Add(MailboxAddress.Parse(correo.Para));
        mensaje.Subject = correo.Asunto;
        mensaje.Body = new BodyBuilder
        {
            TextBody = correo.Texto,
            HtmlBody = PlantillaDeCorreo.Html(correo.Asunto, correo.Texto)
        }.ToMessageBody();
        return mensaje;
    }
}
