using System.Globalization;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Modelo del partial _FechaTile: el bloque de calendario de un turno (mes,
/// día y día de la semana). Es solo visual: la vista que lo usa escribe la
/// fecha completa al lado, que es lo que lee el lector de pantalla.
/// </summary>
public class FechaTileViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public required DateTime Fecha { get; init; }

    /// <summary>La versión grande, para el encabezado del detalle de un turno.</summary>
    public bool Grande { get; init; }

    /// <summary>
    /// "oct". Sin punto, como en <see cref="Services.FechaArgentina.Corta"/>:
    /// según el sistema .NET escribe "oct" u "oct.".
    /// </summary>
    public string Mes => Fecha.ToString("MMM", Cultura).TrimEnd('.');

    public string Dia => Fecha.Day.ToString(CultureInfo.InvariantCulture);

    /// <summary>"lun", también sin punto.</summary>
    public string Semana => Fecha.ToString("ddd", Cultura).TrimEnd('.');
}
