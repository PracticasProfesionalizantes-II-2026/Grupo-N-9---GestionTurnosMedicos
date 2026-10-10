namespace ChronoSaludWeb.Models;

/// <summary>La hoja del comprobante del turno (Turnos/Comprobante).</summary>
public class TurnoComprobanteViewModel
{
    public int IdTurno { get; init; }

    /// <summary>Null si la API falló.</summary>
    public TurnoDetalleViewModel? Turno { get; init; }

    /// <summary>Dirección de la clínica; null mientras no esté cargada en appsettings.json.</summary>
    public string? Direccion { get; init; }

    public DateTime GeneradoEl { get; init; }

    public string GeneradoElTexto => GeneradoEl.ToString("d/M/yyyy, HH:mm", TurnosIndexViewModel.Cultura);

    public string? Error { get; init; }

    public bool HuboError => Error is not null;
}
