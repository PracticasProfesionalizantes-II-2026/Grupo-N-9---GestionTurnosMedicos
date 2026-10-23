namespace ChronoSaludApi.Logica;

/// <summary>
/// "Manda" el email escribiéndolo en el log de la API (la consola en
/// desarrollo). Sirve para probar "Olvidé mi contraseña" y los recordatorios
/// sin un servidor de correo: el enlace se copia de ahí.
///
/// Se usa solo en desarrollo, o fuera de desarrollo si se pide con
/// Correo:MostrarEnLog: el enlace permite cambiar la contraseña, así que el
/// log pasa a tener datos sensibles.
/// </summary>
public class CorreoEnConsola : ITransporteDeCorreo
{
    private readonly ILogger<CorreoEnConsola> _log;

    public CorreoEnConsola(ILogger<CorreoEnConsola> log)
    {
        _log = log;
    }

    public Task Mandar(CorreoSaliente correo, CancellationToken cancelacion)
    {
        _log.LogWarning("Email (no se manda, solo se muestra acá)\nPara: {Para}\nAsunto: {Asunto}\n{Texto}",
            correo.Para, correo.Asunto, correo.Texto);
        return Task.CompletedTask;
    }
}
