using System.Globalization;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Pantalla de reprogramar: el turno como está, la tira de días del mismo
/// doctor y los horarios libres del día elegido. Tocar un horario lleva a la
/// confirmación "de … a …" (<see cref="ConfirmarReprogramacionViewModel"/>).
/// </summary>
public class TurnoReprogramarViewModel
{
    /// <summary>El turno tal como está ahora.</summary>
    public TurnoDetalleViewModel Turno { get; init; } = new();

    public TiraDeDiasViewModel? Tira { get; set; }

    /// <summary>El día elegido en la tira.</summary>
    public DateTime? Fecha { get; set; }

    /// <summary>Horarios libres del doctor ese día.</summary>
    public IReadOnlyList<FranjaViewModel> Franjas { get; set; } = Array.Empty<FranjaViewModel>();

    /// <summary>Error al buscar los días o los horarios, para mostrarlo en su lugar.</summary>
    public string? AvisoFranjas { get; set; }

    /// <summary>Por ejemplo "jueves 9 de octubre".</summary>
    public string? FechaLarga => Fecha?.ToString("dddd d 'de' MMMM", TurnosIndexViewModel.Cultura);

    /// <summary>Lo que conservan los enlaces de la tira: el id del turno.</summary>
    public IDictionary<string, string> RutaBase =>
        new Dictionary<string, string> { ["id"] = $"{Turno.IdTurno}" };

    /// <summary>Los parámetros del enlace de un horario a la confirmación.</summary>
    public IDictionary<string, string> RutaConfirmar(FranjaViewModel franja)
    {
        var ruta = new Dictionary<string, string>
        {
            ["id"] = $"{Turno.IdTurno}",
            ["inicio"] = franja.HoraInicio,
            ["fin"] = franja.HoraFin
        };

        if (Fecha is { } dia) ruta["fecha"] = dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return ruta;
    }
}

/// <summary>
/// Confirmación de la reprogramación: de qué día y hora a cuáles se mueve el
/// turno. Recién al confirmar se le pide el cambio a la API.
/// </summary>
public class ConfirmarReprogramacionViewModel
{
    /// <summary>El turno tal como está ahora.</summary>
    public TurnoDetalleViewModel Turno { get; init; } = new();

    // El día y el horario nuevos.
    public DateTime Fecha { get; init; }
    public string HoraInicio { get; init; } = string.Empty;
    public string HoraFin { get; init; } = string.Empty;

    /// <summary>Mensaje de la API si no se pudo reprogramar (por ejemplo, el horario se ocupó).</summary>
    public string? Error { get; init; }

    /// <summary>Por ejemplo "jueves 9 de octubre".</summary>
    public string FechaActualLarga => Turno.FechaInicio.ToString("dddd d 'de' MMMM", TurnosIndexViewModel.Cultura);

    public string FechaNuevaLarga => Fecha.ToString("dddd d 'de' MMMM", TurnosIndexViewModel.Cultura);

    public string HorarioNuevo => $"{HoraInicio} a {HoraFin}";

    /// <summary>La fecha nueva como la espera el campo oculto.</summary>
    public string FechaIso => Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>"Elegir otro horario": vuelve a la tira, en el mismo día.</summary>
    public IDictionary<string, string> RutaElegirOtro =>
        new Dictionary<string, string> { ["id"] = $"{Turno.IdTurno}", ["fecha"] = FechaIso };
}
