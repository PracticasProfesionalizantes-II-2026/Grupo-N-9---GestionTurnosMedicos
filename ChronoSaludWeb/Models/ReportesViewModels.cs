using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>Un período del reporte: los dos días van incluidos.</summary>
public record Periodo(string Clave, DateTime Desde, DateTime Hasta);

/// <summary>
/// Los períodos con nombre de la pantalla Reportes ("Esta semana", "Este
/// mes"…) y las fechas elegidas a mano. La semana va de lunes a domingo.
/// </summary>
public static class PeriodoDeReporte
{
    public const string EstaSemana = "semana";
    public const string SemanaPasada = "semana-pasada";
    public const string EsteMes = "mes";
    public const string MesPasado = "mes-pasado";

    /// <summary>Fechas elegidas a mano: no viaja como "periodo" sino como desde y hasta.</summary>
    public const string Elegido = "fechas";

    /// <summary>Lo mismo que acepta la API: un año, bisiesto incluido.</summary>
    public const int DiasMaximos = 366;

    public static readonly IReadOnlyList<(string Clave, string Titulo)> Opciones = new[]
    {
        (EstaSemana, "Esta semana"),
        (SemanaPasada, "Semana pasada"),
        (EsteMes, "Este mes"),
        (MesPasado, "Mes pasado"),
    };

    /// <summary>
    /// Un período con nombre gana sobre las fechas; sin ninguno de los dos
    /// va "Este mes". Si llega una sola fecha, el período es ese día.
    /// </summary>
    public static Periodo Resolver(string? periodo, DateTime? desde, DateTime? hasta, DateTime hoy)
    {
        hoy = hoy.Date;
        var lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
        var primeroDelMes = new DateTime(hoy.Year, hoy.Month, 1);

        switch (periodo)
        {
            case EstaSemana:   return new Periodo(EstaSemana, lunes, lunes.AddDays(6));
            case SemanaPasada: return new Periodo(SemanaPasada, lunes.AddDays(-7), lunes.AddDays(-1));
            case EsteMes:      return new Periodo(EsteMes, primeroDelMes, primeroDelMes.AddMonths(1).AddDays(-1));
            case MesPasado:    return new Periodo(MesPasado, primeroDelMes.AddMonths(-1), primeroDelMes.AddDays(-1));
        }

        if (!desde.HasValue && !hasta.HasValue)
            return Resolver(EsteMes, null, null, hoy);

        // Unas fechas que son justo las de un período con nombre (pasa al
        // cambiar solo el doctor con el formulario) se muestran como ese período.
        var inicio = (desde ?? hasta)!.Value.Date;
        var fin = (hasta ?? desde)!.Value.Date;
        foreach (var (clave, _) in Opciones)
        {
            var conNombre = Resolver(clave, null, null, hoy);
            if (conNombre.Desde == inicio && conNombre.Hasta == fin)
                return conNombre;
        }

        return new Periodo(Elegido, inicio, fin);
    }

    /// <summary>Qué tiene de malo un período elegido a mano; null si está bien.</summary>
    public static string? Problema(Periodo periodo)
    {
        if (periodo.Hasta < periodo.Desde)
            return "La fecha «hasta» es anterior a la «desde». Revisalas y probá de nuevo.";
        if ((periodo.Hasta - periodo.Desde).TotalDays >= DiasMaximos)
            return "Elegí un período de un año como máximo.";
        return null;
    }

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>
    /// "Del 1 al 31 de octubre de 2026", "Del 28 de septiembre al 4 de octubre
    /// de 2026", "Del 29 de diciembre de 2026 al 4 de enero de 2027" o, si es
    /// un solo día, "El 13 de octubre de 2026".
    /// </summary>
    public static string Texto(DateTime desde, DateTime hasta)
    {
        string Dia(DateTime f) => f.Day.ToString(Cultura);
        string Mes(DateTime f) => f.ToString("MMMM", Cultura);

        if (desde.Date == hasta.Date)
            return $"El {Dia(desde)} de {Mes(desde)} de {desde.Year}";
        if (desde.Year != hasta.Year)
            return $"Del {Dia(desde)} de {Mes(desde)} de {desde.Year} al {Dia(hasta)} de {Mes(hasta)} de {hasta.Year}";
        if (desde.Month != hasta.Month)
            return $"Del {Dia(desde)} de {Mes(desde)} al {Dia(hasta)} de {Mes(hasta)} de {hasta.Year}";
        return $"Del {Dia(desde)} al {Dia(hasta)} de {Mes(hasta)} de {hasta.Year}";
    }
}

/// <summary>Una barra del reporte por estado: cuántos y qué parte del total.</summary>
public record BarraDeEstado(string Etiqueta, int Cantidad, int Porcentaje, string Tono);

/// <summary>
/// La pantalla Reportes: "Reportes" para el administrador (toda la clínica,
/// con filtro por doctor y especialidad) y "Mis reportes" para el doctor
/// (solo lo suyo; la API lo saca del token).
/// </summary>
public class ReportesViewModel
{
    public bool EsPropia { get; init; }
    public required Periodo Periodo { get; init; }
    public int? Doctor { get; init; }
    public string? Especialidad { get; init; }

    public IReadOnlyList<SelectListItem> Doctores { get; init; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Especialidades { get; init; } = Array.Empty<SelectListItem>();

    public ReporteResumen? Reporte { get; init; }

    /// <summary>Las fechas elegidas a mano no sirven: se dice qué pasa y no se pide nada.</summary>
    public string? ProblemaDelPeriodo { get; init; }

    /// <summary>No se pudo cargar (la API falló o no respondió).</summary>
    public string? Error { get; init; }

    /// <summary>No es una falla: un doctor sin perfil completo.</summary>
    public string? Aviso { get; init; }

    public string Titulo => EsPropia ? "Mis reportes" : "Reportes";

    public string TextoDelPeriodo => PeriodoDeReporte.Texto(Periodo.Desde, Periodo.Hasta);

    public bool HayFiltro => Doctor is not null || Especialidad is not null;

    /// <summary>Hay reporte y tuvo al menos un turno.</summary>
    public bool HayTurnos => Reporte is { Turnos.Total: > 0 };

    /// <summary>El nombre del doctor o la especialidad elegidos, para el subtítulo y el CSV.</summary>
    public string? DeQuien
    {
        get
        {
            var doctor = Doctor is { } id ? Doctores.FirstOrDefault(d => d.Value == $"{id}")?.Text : null;
            return (doctor, Especialidad) switch
            {
                (not null, not null) => $"{doctor} ({Especialidad})",
                (not null, null) => doctor,
                (null, not null) => Especialidad,
                _ => null
            };
        }
    }

    /// <summary>
    /// Las barras de cada estado, en el orden en que avanza un turno. El tono
    /// es el mismo del chip del estado; ausente va neutro, como su chip.
    /// </summary>
    public IReadOnlyList<BarraDeEstado> Estados
    {
        get
        {
            if (Reporte is null) return Array.Empty<BarraDeEstado>();
            var t = Reporte.Turnos;
            return new[]
            {
                new BarraDeEstado("Pendientes", t.Pendientes, Porcentaje(t.Pendientes, t.Total), "aviso"),
                new BarraDeEstado("Confirmados", t.Confirmados, Porcentaje(t.Confirmados, t.Total), "exito"),
                new BarraDeEstado("Completados", t.Completados, Porcentaje(t.Completados, t.Total), "info"),
                new BarraDeEstado("Ausentes", t.Ausentes, Porcentaje(t.Ausentes, t.Total), "neutro"),
                new BarraDeEstado("Cancelados", t.Cancelados, Porcentaje(t.Cancelados, t.Total), "peligro"),
            };
        }
    }

    /// <summary>
    /// Qué parte de <paramref name="total"/> es <paramref name="valor"/>, de 0
    /// a 100. Un valor que no es cero nunca queda en 0, para que su barra se vea.
    /// </summary>
    public static int Porcentaje(int valor, int total)
    {
        if (total <= 0 || valor <= 0) return 0;
        return Math.Clamp((int)Math.Round(100.0 * valor / total, MidpointRounding.AwayFromZero), 1, 100);
    }

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>"25 %", "33,3 %" o "—" si no hubo turnos cerrados.</summary>
    public static string TextoDeAusentismo(double? ausentismo) =>
        ausentismo is { } valor ? $"{valor.ToString("0.#", Cultura)} %" : "—";

    /// <summary>La ruta de un período con nombre, con los filtros que haya.</summary>
    public Dictionary<string, string> RutaDe(string periodo) => Filtros(new Dictionary<string, string> { ["periodo"] = periodo });

    /// <summary>La ruta de lo que se está viendo: para "Descargar CSV" y "Reintentar".</summary>
    public Dictionary<string, string> RutaActual() =>
        Periodo.Clave == PeriodoDeReporte.Elegido
            ? Filtros(new Dictionary<string, string>
            {
                ["desde"] = Periodo.Desde.ToString("yyyy-MM-dd"),
                ["hasta"] = Periodo.Hasta.ToString("yyyy-MM-dd")
            })
            : RutaDe(Periodo.Clave);

    private Dictionary<string, string> Filtros(Dictionary<string, string> ruta)
    {
        if (Doctor is { } id) ruta["doctor"] = $"{id}";
        if (Especialidad is { } especialidad) ruta["especialidad"] = especialidad;
        return ruta;
    }
}
