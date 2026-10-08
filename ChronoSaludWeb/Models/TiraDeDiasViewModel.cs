using System.Globalization;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Un día de la tira de días (Shared/_TiraDeDias): la fecha, cuántos horarios
/// libres tiene y a dónde lleva al tocarlo.
/// </summary>
public class DiaDeLaTira
{
    public DateTime Fecha { get; init; }

    /// <summary>
    /// Cuántos horarios libres tiene. Null si la API no lo informó (por
    /// ejemplo, una versión anterior): el día se muestra igual, para elegirlo.
    /// </summary>
    public int? Libres { get; init; }

    /// <summary>Es el día que se está mirando.</summary>
    public bool Elegido { get; init; }

    /// <summary>Los parámetros del enlace del día.</summary>
    public IDictionary<string, string> Ruta { get; init; } = new Dictionary<string, string>();

    /// <summary>Un día sin lugar no es un enlace: no hay nada para elegir.</summary>
    public bool SinLugar => Libres == 0;

    /// <summary>"jue", sin el punto que pone la cultura es-AR.</summary>
    public string DiaCorto => Fecha.ToString("ddd", TurnosIndexViewModel.Cultura).TrimEnd('.');

    /// <summary>"9 oct".</summary>
    public string FechaCorta => $"{Fecha.Day} {Fecha.ToString("MMM", TurnosIndexViewModel.Cultura).TrimEnd('.')}";

    public string TextoLibres
    {
        get
        {
            if (Libres is null) return "Ver";
            if (Libres == 0) return "Sin lugar";
            if (Libres == 1) return "1 libre";
            return $"{Libres} libres";
        }
    }

    /// <summary>Lo que lee un lector de pantalla: "jueves 9 de octubre, 3 libres".</summary>
    public string Descripcion =>
        $"{Fecha.ToString("dddd d 'de' MMMM", TurnosIndexViewModel.Cultura)}, {TextoLibres.ToLowerInvariant()}";
}

/// <summary>
/// La tira de días con lugar: dos semanas, y enlaces a las dos anteriores y
/// a las dos siguientes. La usan "Pedir turno" y, más adelante, "Reprogramar".
/// </summary>
public class TiraDeDiasViewModel
{
    /// <summary>La acción del controlador actual a la que llevan los enlaces.</summary>
    public string Accion { get; init; } = "Crear";

    public IReadOnlyList<DiaDeLaTira> Dias { get; init; } = Array.Empty<DiaDeLaTira>();

    /// <summary>Null si la tira ya arranca hoy: antes no hay nada para reservar.</summary>
    public IDictionary<string, string>? RutaAnteriores { get; init; }

    public IDictionary<string, string> RutaSiguientes { get; init; } = new Dictionary<string, string>();

    /// <summary>Ningún día de la tira tiene lugar (y se sabe: la API mandó los números).</summary>
    public bool SinLugarEnNingunDia => Dias.Count > 0 && Dias.All(d => d.SinLugar);

    /// <summary>El día que se está mirando, o null si no hay ninguno elegido.</summary>
    public DateTime? DiaElegido => Dias.FirstOrDefault(d => d.Elegido)?.Fecha;

    /// <summary>
    /// Una copia de <paramref name="rutaBase"/> (lo que la pantalla necesita
    /// conservar: el id del turno, la especialidad...) con el día y el
    /// comienzo de la tira. Lo que va en null no viaja.
    /// </summary>
    public static IDictionary<string, string> RutaConFechas(
        IDictionary<string, string> rutaBase, DateTime? fecha, DateTime? desde)
    {
        var ruta = new Dictionary<string, string>(rutaBase);

        if (fecha is { } dia) ruta["fecha"] = dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (desde is { } inicio) ruta["desde"] = inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return ruta;
    }
}
