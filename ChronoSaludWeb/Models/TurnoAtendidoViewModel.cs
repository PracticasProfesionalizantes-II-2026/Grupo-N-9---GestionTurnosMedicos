using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>
/// El turno desde el que el doctor registra una consulta o emite una receta.
/// Se muestra arriba del formulario; lo único que viaja en el formulario es
/// su id.
/// </summary>
public class TurnoAtendidoViewModel
{
    public int IdTurno { get; init; }
    public int IdPaciente { get; init; }
    public DateTime FechaInicio { get; init; }
    public string HoraInicio { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;

    /// <summary>Se puede marcar como completado: está en pie y ya empezó.</summary>
    public bool PuedeCompletarse { get; init; }

    /// <summary>No está cancelado ni ausente: ahí no hubo atención.</summary>
    public bool SePuedeAtender { get; init; }

    /// <summary>"turno #12 del 9/10/2026 a las 10:00", para ir en medio de una frase.</summary>
    public string Descripcion =>
        $"turno #{IdTurno} del {FechaInicio.ToString("d/M/yyyy", TurnosIndexViewModel.Cultura)} a las {HoraInicio}";

    public static TurnoAtendidoViewModel Desde(TurnoDetalle turno)
    {
        // Las reglas de estado y de hora son las mismas del detalle del turno.
        var detalle = new TurnoDetalleViewModel
        {
            Estado = turno.Estado,
            FechaInicio = turno.FechaInicio,
            HoraInicio = turno.HoraInicio
        };

        return new TurnoAtendidoViewModel
        {
            IdTurno = turno.IdTurno,
            IdPaciente = turno.IdPaciente,
            FechaInicio = turno.FechaInicio,
            HoraInicio = turno.HoraInicio,
            Estado = turno.Estado,
            PuedeCompletarse = detalle.PuedeCompletarse,
            SePuedeAtender = detalle.SePuedeAtender
        };
    }
}
