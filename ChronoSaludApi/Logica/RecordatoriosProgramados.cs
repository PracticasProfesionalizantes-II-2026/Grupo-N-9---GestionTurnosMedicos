using Microsoft.Extensions.Options;

namespace ChronoSaludApi.Logica;

/// <summary>La sección "Recordatorios" de la configuración.</summary>
public class OpcionesDeRecordatorios
{
    public bool Activos { get; set; } = true;

    /// <summary>Desde qué hora de Argentina se mandan los del día, "HH:mm".</summary>
    public string Hora { get; set; } = "07:00";

    /// <summary>
    /// Cada cuánto se vuelve a mirar. Después de la hora de envío, cada
    /// revisión manda solo lo que falte: un turno sacado a media mañana para
    /// la tarde también recibe el suyo.
    /// </summary>
    public int MinutosEntreRevisiones { get; set; } = 15;

    public TimeSpan HoraDeEnvio => TimeSpan.TryParse(Hora, out var hora) ? hora : TimeSpan.FromHours(7);
}

/// <summary>
/// Dispara <see cref="RecordatorioDeTurnos"/> cada tantos minutos desde la
/// hora de envío. Corre dentro de la API: en Azure, el App Service tiene que
/// tener "Always On" activado, porque si no la API se duerme cuando nadie la
/// usa y no revisa nada (ver README, "Correo y recordatorios").
/// </summary>
public class RecordatoriosProgramados : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly OpcionesDeRecordatorios _opciones;
    private readonly IReloj _reloj;
    private readonly ILogger<RecordatoriosProgramados> _log;

    public RecordatoriosProgramados(
        IServiceScopeFactory scopes,
        IOptions<OpcionesDeRecordatorios> opciones,
        IReloj reloj,
        ILogger<RecordatoriosProgramados> log)
    {
        _scopes = scopes;
        _opciones = opciones.Value;
        _reloj = reloj;
        _log = log;
    }

    public static bool TocaRevisar(DateTime ahora, TimeSpan horaDeEnvio) => ahora.TimeOfDay >= horaDeEnvio;

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        if (!_opciones.Activos)
        {
            _log.LogInformation("Los recordatorios de turnos están apagados (Recordatorios:Activos).");
            return;
        }

        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _opciones.MinutosEntreRevisiones)));
        try
        {
            do
            {
                await Revisar(cancelacion);
            }
            while (await temporizador.WaitForNextTickAsync(cancelacion));
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            // La API se está apagando.
        }
    }

    private async Task Revisar(CancellationToken cancelacion)
    {
        if (!TocaRevisar(_reloj.Ahora(), _opciones.HoraDeEnvio))
            return;

        try
        {
            // La lógica y los repositorios son de un pedido (Scoped): acá no
            // hay pedido, así que se arma uno por revisión.
            using var scope = _scopes.CreateScope();
            var recordatorio = scope.ServiceProvider.GetRequiredService<IRecordatorioDeTurnos>();
            await recordatorio.EnviarLosDeHoy(cancelacion);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Una base caída no tiene que tirar la API: se prueba en la próxima vuelta.
            _log.LogError(error, "No se pudieron revisar los recordatorios de turnos.");
        }
    }
}
