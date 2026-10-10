namespace ChronoSaludApi.Logica;

/// <summary>
/// Manda un email. Hoy la única implementación es <see cref="CorreoEnConsola"/>,
/// que lo escribe en el log; para mandarlo de verdad (SMTP, SendGrid, Azure
/// Communication Services…) alcanza con otra clase que implemente esto y
/// cambiar una línea en Program.cs.
/// </summary>
public interface IEnviadorDeCorreo
{
    Task Enviar(string para, string asunto, string texto);
}
