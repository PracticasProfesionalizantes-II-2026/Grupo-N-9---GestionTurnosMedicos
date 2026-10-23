namespace ChronoSaludApi.Logica;

/// <summary>
/// Manda un email. La lógica solo ve esto. La implementación de la aplicación
/// es <see cref="ColaDeCorreos"/>: deja el correo en una cola y vuelve, y
/// <see cref="EnvioDeCorreos"/> lo manda en segundo plano con el transporte
/// que diga la configuración (SMTP, el log en desarrollo, o ninguno). Las
/// pruebas usan uno falso que guarda lo enviado.
/// </summary>
public interface IEnviadorDeCorreo
{
    Task Enviar(string para, string asunto, string texto);
}
