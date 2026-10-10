namespace ChronoSaludApi.Logica;

/// <summary>
/// "Manda" el email escribiéndolo en el log de la API (la consola en
/// desarrollo; el Log stream en Azure). Sirve para probar "Olvidé mi
/// contraseña" sin un servidor de correo: el enlace se copia de ahí.
///
/// OJO: el enlace permite cambiar la contraseña, así que el log pasa a tener
/// datos sensibles. Es una solución de mientras, hasta conectar un proveedor.
/// </summary>
public class CorreoEnConsola : IEnviadorDeCorreo
{
    private readonly ILogger<CorreoEnConsola> _log;

    public CorreoEnConsola(ILogger<CorreoEnConsola> log)
    {
        _log = log;
    }

    public Task Enviar(string para, string asunto, string texto)
    {
        _log.LogWarning("Email (no se manda, solo se muestra acá)\nPara: {Para}\nAsunto: {Asunto}\n{Texto}",
            para, asunto, texto);
        return Task.CompletedTask;
    }
}
