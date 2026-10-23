namespace ChronoSaludApi.Logica;

/// <summary>
/// Saca los correos de la <see cref="ColaDeCorreos"/> y los manda con el
/// transporte configurado. Si el servidor falla, reintenta dos veces (a los 10
/// segundos y al minuto); después lo deja y lo anota en el log, sin el
/// contenido del correo.
/// </summary>
public class EnvioDeCorreos : BackgroundService
{
    public static readonly TimeSpan[] EsperasPorDefecto = [TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)];

    private readonly ColaDeCorreos _cola;
    private readonly ITransporteDeCorreo _transporte;
    private readonly ILogger<EnvioDeCorreos> _log;
    private readonly TimeSpan[] _esperas;

    /// <param name="esperas">Las esperas entre reintentos. Las pruebas pasan unas cortas.</param>
    public EnvioDeCorreos(ColaDeCorreos cola, ITransporteDeCorreo transporte, ILogger<EnvioDeCorreos> log,
        TimeSpan[]? esperas = null)
    {
        _cola = cola;
        _transporte = transporte;
        _log = log;
        _esperas = esperas ?? EsperasPorDefecto;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        await foreach (var correo in _cola.Pendientes.ReadAllAsync(cancelacion))
            await MandarConReintentos(correo, cancelacion);
    }

    private async Task MandarConReintentos(CorreoSaliente correo, CancellationToken cancelacion)
    {
        for (var intento = 0; ; intento++)
        {
            try
            {
                await _transporte.Mandar(correo, cancelacion);
                return;
            }
            catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error) when (intento < _esperas.Length)
            {
                _log.LogWarning(error, "Falló el envío del email «{Asunto}» a {Para}; se reintenta en {Espera}.",
                    correo.Asunto, EleccionDeTransporte.Enmascarar(correo.Para), _esperas[intento]);
                await Task.Delay(_esperas[intento], cancelacion);
            }
            catch (Exception error)
            {
                _log.LogError(error, "No se pudo mandar el email «{Asunto}» a {Para} después de {Intentos} intentos.",
                    correo.Asunto, EleccionDeTransporte.Enmascarar(correo.Para), intento + 1);
                return;
            }
        }
    }
}
