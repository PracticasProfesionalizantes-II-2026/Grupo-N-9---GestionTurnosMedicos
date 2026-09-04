using System.Globalization;

namespace ChronoSaludWeb.Models;

public class TurnoFilaViewModel
{
    public int IdTurno { get; init; }
    public DateTime FechaInicio { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Paciente { get; init; } = string.Empty;
    public string Doctor { get; init; } = string.Empty;

    /// <summary>
    /// Null cuando la API no mandó la hora. GET /turnos devuelve FechaInicio
    /// a las 00:00 porque la hora vive en otra columna que el listado no expone.
    /// </summary>
    public string? Hora { get; init; }

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(Paciente);

    public string PacienteMostrado =>
        string.IsNullOrWhiteSpace(Paciente) ? "Paciente sin datos" : Paciente.Trim();

    public string DoctorMostrado =>
        string.IsNullOrWhiteSpace(Doctor) ? "Doctor sin asignar" : Doctor.Trim();

    public string FechaCorta =>
        FechaInicio.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);
}

public class TurnosIndexViewModel
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public IReadOnlyList<TurnoFilaViewModel> Turnos { get; init; } = Array.Empty<TurnoFilaViewModel>();

    /// <summary>Total que informa la API, puede ser mayor que lo listado.</summary>
    public int Total { get; init; }

    /// <summary>Mensaje de error de la API, si la carga falló.</summary>
    public string? Error { get; init; }

    public bool HuboError => Error is not null;

    public int Confirmados => Contar("confirmado");
    public int Pendientes  => Contar("pendiente");
    public int Cancelados  => Contar("cancelado");

    /// <summary>La API paginó y quedaron turnos afuera del listado.</summary>
    public bool HayMas => Total > Turnos.Count;

    /// <summary>Alguna fila vino sin hora desde la API.</summary>
    public bool FaltaHora => Turnos.Any(t => t.Hora is null);

    public string FechaDeHoy =>
        DateTime.Today.ToString("D", Cultura);

    private int Contar(string estado) =>
        Turnos.Count(t => string.Equals(t.Estado, estado, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// "Pedro Paciente" -> "PP". Devuelve "?" si el nombre vino vacío, que pasa
    /// cuando la API no pudo resolver el usuario de la relación.
    /// </summary>
    public static string CalcularIniciales(string nombre)
    {
        var partes = (nombre ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return "?";

        var iniciales = partes.Length == 1
            ? partes[0][..1]
            : string.Concat(partes[0][..1], partes[^1][..1]);

        return iniciales.ToUpperInvariant();
    }
}
