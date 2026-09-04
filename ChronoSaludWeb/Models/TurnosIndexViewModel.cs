using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

public class TurnoFilaViewModel
{
    public int IdTurno { get; init; }
    public DateTime FechaInicio { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Paciente { get; init; } = string.Empty;
    public string Doctor { get; init; } = string.Empty;
    public string Especialidad { get; init; } = string.Empty;

    /// <summary>
    /// Null cuando la API no mandó la hora. Viene en HoraInicio ("HH:mm"),
    /// aparte de FechaInicio, que el listado devuelve a las 00:00.
    /// </summary>
    public string? Hora { get; init; }

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(Paciente);

    public string PacienteMostrado =>
        string.IsNullOrWhiteSpace(Paciente) ? "Paciente sin datos" : Paciente.Trim();

    public string DoctorMostrado =>
        string.IsNullOrWhiteSpace(Doctor) ? "Doctor sin asignar" : Doctor.Trim();

    public string? EspecialidadMostrada =>
        string.IsNullOrWhiteSpace(Especialidad) ? null : Especialidad.Trim();

    public string FechaCorta =>
        FechaInicio.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);

    /// <summary>
    /// Arma la fila a partir de lo que devuelve la API.
    /// </summary>
    public static TurnoFilaViewModel Desde(TurnoLista turno) => new()
    {
        IdTurno      = turno.IdTurno,
        FechaInicio  = turno.FechaInicio,
        Estado       = turno.Estado,
        Paciente     = turno.Paciente,
        Doctor       = turno.Doctor,
        Especialidad = turno.Especialidad,
        // La API manda la hora aparte de FechaInicio (que viene a las 00:00).
        // Si llega vacía no inventamos una: la vista muestra un guion.
        Hora = string.IsNullOrWhiteSpace(turno.HoraInicio) ? null : turno.HoraInicio
    };

    /// <summary>Ya está cancelado: no se ofrece la acción de cancelar.</summary>
    public bool EstaCancelado =>
        string.Equals(Estado, "cancelado", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Filtros del listado. Viajan por query string y vuelven a la vista para que
/// el formulario quede con lo que el usuario eligió.
/// </summary>
public class TurnosFiltroViewModel
{
    /// <summary>Los cuatro estados que maneja la API.</summary>
    public static readonly string[] EstadosTurno =
        { "pendiente", "confirmado", "completado", "cancelado" };

    public string? Estado { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public bool HayAlguno => !string.IsNullOrWhiteSpace(Estado) || Desde is not null || Hasta is not null;

    /// <summary>El rango está al revés y por eso no va a traer nada.</summary>
    public bool RangoInvertido => Desde is not null && Hasta is not null && Desde > Hasta;

    public IEnumerable<SelectListItem> Opciones() =>
        EstadosTurno.Select(e => new SelectListItem(e, e, string.Equals(e, Estado, StringComparison.OrdinalIgnoreCase)));
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

    public TurnosFiltroViewModel Filtros { get; init; } = new();

    public int Confirmados => Contar("confirmado");
    public int Pendientes  => Contar("pendiente");
    public int Completados => Contar("completado");
    public int Cancelados  => Contar("cancelado");

    /// <summary>No hay resultados, pero porque los filtros no matchean nada.</summary>
    public bool SinResultados => Turnos.Count == 0 && Filtros.HayAlguno;

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
