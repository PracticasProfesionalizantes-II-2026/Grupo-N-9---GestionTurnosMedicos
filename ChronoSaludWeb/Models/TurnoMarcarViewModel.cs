namespace ChronoSaludWeb.Models;

/// <summary>
/// La confirmación antes de marcar un turno como completado o ausente
/// (Turnos/Marcar). Los dos estados son definitivos: después no se pueden
/// cambiar, por eso se pregunta antes, igual que al cancelar.
/// </summary>
public class TurnoMarcarViewModel
{
    /// <summary>Los estados que se confirman en esta pantalla.</summary>
    public static readonly string[] Estados = ["completado", "ausente"];

    public required TurnoDetalleViewModel Turno { get; init; }

    /// <summary>"completado" o "ausente".</summary>
    public required string Estado { get; init; }

    /// <summary>
    /// El listado del que se vino, ya validado como URL local. Null si se vino
    /// del detalle del turno.
    /// </summary>
    public string? Volver { get; init; }

    public bool EsAusente => Estado == "ausente";

    public static bool EsEstadoValido(string? estado) => estado is not null && Estados.Contains(estado);

    public string Titulo => EsAusente
        ? $"¿Marcar ausente el turno #{Turno.IdTurno}?"
        : $"¿Marcar como completado el turno #{Turno.IdTurno}?";

    /// <summary>"El turno de Ana Duarte del viernes 9 de octubre de 2026 a las 10:00 …"</summary>
    public string Pregunta
    {
        get
        {
            // Con el mes completo y no con el formato "D", que cambia según el sistema.
            var fecha = Turno.FechaInicio.ToString("dddd d 'de' MMMM 'de' yyyy", TurnosIndexViewModel.Cultura);
            var turno = $"El turno de {Turno.PacienteMostrado} del {fecha} a las {Turno.HoraInicio}";
            return EsAusente
                ? $"{turno} queda como ausente: el paciente no vino."
                : $"{turno} queda como completado: el paciente fue atendido.";
        }
    }

    public string TextoBoton => EsAusente ? "Sí, marcar ausente" : "Sí, marcar como completado";
}
