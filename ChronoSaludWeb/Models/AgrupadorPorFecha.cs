using System.Globalization;

namespace ChronoSaludWeb.Models;

/// <summary>Un grupo de la línea de tiempo: su título ("Hoy", "Octubre de 2026") y lo que cae adentro.</summary>
public record GrupoDeFechas<T>(string Titulo, IReadOnlyList<T> Elementos);

/// <summary>
/// Arma los grupos de una línea de tiempo (historial clínico, actividad,
/// notificaciones). Respeta el orden en que llegan los elementos: junta los
/// que están seguidos y caen el mismo día (o mes), así un listado que ya
/// viene ordenado por fecha sale igual, con un título cada vez que cambia.
/// </summary>
public static class AgrupadorPorFecha
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>
    /// Por día: "Hoy", "Ayer", y si no "Lunes 12 de octubre" (con el año si
    /// no es el actual). Lo que no tiene fecha va al final, en "Sin fecha".
    /// </summary>
    public static IReadOnlyList<GrupoDeFechas<T>> PorDia<T>(IEnumerable<T> elementos, Func<T, DateTime?> fecha, DateTime hoy) =>
        Agrupar(elementos, e => fecha(e)?.Date, dia => TituloDelDia(dia, hoy.Date));

    /// <summary>Por mes: "Octubre de 2026". Lo que no tiene fecha va al final, en "Sin fecha".</summary>
    public static IReadOnlyList<GrupoDeFechas<T>> PorMes<T>(IEnumerable<T> elementos, Func<T, DateTime?> fecha) =>
        Agrupar(elementos,
                e => fecha(e) is { } f ? new DateTime(f.Year, f.Month, 1) : null,
                mes => Capitalizar(mes.ToString("MMMM 'de' yyyy", Cultura)));

    public static string TituloDelDia(DateTime dia, DateTime hoy)
    {
        if (dia == hoy) return "Hoy";
        if (dia == hoy.AddDays(-1)) return "Ayer";

        var texto = dia.ToString("dddd d 'de' MMMM", Cultura);
        return Capitalizar(dia.Year == hoy.Year ? texto : $"{texto} de {dia.Year}");
    }

    private static IReadOnlyList<GrupoDeFechas<T>> Agrupar<T>(IEnumerable<T> elementos, Func<T, DateTime?> clave, Func<DateTime, string> titulo)
    {
        var grupos = new List<GrupoDeFechas<T>>();
        var sinFecha = new List<T>();
        DateTime? claveActual = null;
        List<T>? actual = null;

        foreach (var elemento in elementos)
        {
            if (clave(elemento) is not { } k)
            {
                sinFecha.Add(elemento);
                continue;
            }

            if (actual is null || k != claveActual)
            {
                actual = [];
                claveActual = k;
                grupos.Add(new GrupoDeFechas<T>(titulo(k), actual));
            }

            actual.Add(elemento);
        }

        if (sinFecha.Count > 0)
            grupos.Add(new GrupoDeFechas<T>("Sin fecha", sinFecha));

        return grupos;
    }

    private static string Capitalizar(string texto) =>
        texto.Length == 0 ? texto : char.ToUpper(texto[0], Cultura) + texto[1..];
}
