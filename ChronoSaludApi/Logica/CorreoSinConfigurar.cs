namespace ChronoSaludApi.Logica;

/// <summary>
/// Fuera de desarrollo y sin servidor SMTP: el correo no sale y tampoco se
/// escribe en el log (puede traer un enlace para cambiar la contraseña). Solo
/// queda un aviso de que falta configurar el correo, sin el contenido.
/// </summary>
public class CorreoSinConfigurar : ITransporteDeCorreo
{
    private readonly ILogger<CorreoSinConfigurar> _log;

    public CorreoSinConfigurar(ILogger<CorreoSinConfigurar> log)
    {
        _log = log;
    }

    public Task Mandar(CorreoSaliente correo, CancellationToken cancelacion)
    {
        _log.LogWarning(
            "No se mandó el email «{Asunto}» a {Para}: falta configurar el servidor de correo (Correo:Smtp:Host).",
            correo.Asunto, EleccionDeTransporte.Enmascarar(correo.Para));
        return Task.CompletedTask;
    }
}
