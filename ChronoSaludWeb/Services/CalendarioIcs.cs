using System.Globalization;
using System.Text;
using ChronoSaludWeb.Models;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Arma el archivo .ics de un turno: el formato de calendario que entienden
/// el celular (Android, iPhone) y la compu (Google Calendar, Outlook). Al
/// abrirlo, el turno queda agendado con dos avisos: el día anterior y una
/// hora antes. Es el recordatorio más simple, sin mandar emails ni nada
/// automático desde el servidor.
/// </summary>
public static class CalendarioIcs
{
    /// <summary>Si la API no mandó la hora de fin, el turno dura esto.</summary>
    public static readonly TimeSpan DuracionPorDefecto = TimeSpan.FromMinutes(30);

    // El formato pide cortar las líneas a 75 caracteres (RFC 5545).
    private const int LargoMaximo = 75;

    public static string Armar(TurnoDetalleViewModel turno, string? direccion, DateTime ahoraUtc)
    {
        var inicio = HoraArgentina(turno.FechaInicio, turno.HoraInicio) ?? turno.FechaInicio.Date;
        var fin = HoraArgentina(turno.FechaInicio, turno.HoraFin) ?? inicio + DuracionPorDefecto;

        // Un fin que no tiene sentido (igual o antes del inicio) se corrige.
        if (fin <= inicio)
            fin = inicio + DuracionPorDefecto;

        var titulo = string.IsNullOrWhiteSpace(turno.Especialidad)
            ? $"Turno con {turno.DoctorMostrado}"
            : $"Turno con {turno.DoctorMostrado} ({turno.Especialidad.Trim()})";

        var lugar = string.Join(", ", new[]
        {
            ConsultorioTexto.Para(turno.Consultorio),
            string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim()
        }.Where(parte => parte is not null));

        var descripcion = $"Turno #{turno.IdTurno} en ChronoSalud. Llevá tu DNI y la credencial de tu cobertura.";

        var lineas = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//ChronoSalud//Turnos//ES",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            // El mismo UID para el mismo turno: si se descarga dos veces, el
            // calendario lo actualiza en vez de duplicarlo.
            $"UID:turno-{turno.IdTurno}@chronosalud",
            $"DTSTAMP:{EnUtc(ahoraUtc)}",
            $"DTSTART:{EnUtc(AUtc(inicio))}",
            $"DTEND:{EnUtc(AUtc(fin))}",
            $"SUMMARY:{Escapar(titulo)}",
            $"DESCRIPTION:{Escapar(descripcion)}"
        };

        if (lugar.Length > 0)
            lineas.Add($"LOCATION:{Escapar(lugar)}");

        // Los dos avisos: un día antes y una hora antes.
        lineas.AddRange(Aviso("-P1D", $"Mañana: {titulo}"));
        lineas.AddRange(Aviso("-PT1H", $"En una hora: {titulo}"));

        lineas.Add("END:VEVENT");
        lineas.Add("END:VCALENDAR");

        // Cada línea termina en CRLF, como pide el formato.
        var texto = new StringBuilder();
        foreach (var linea in lineas)
        {
            foreach (var parte in Cortar(linea))
                texto.Append(parte).Append("\r\n");
        }

        return texto.ToString();
    }

    private static IEnumerable<string> Aviso(string cuandoAntes, string mensaje) =>
    [
        "BEGIN:VALARM",
        "ACTION:DISPLAY",
        $"TRIGGER:{cuandoAntes}",
        $"DESCRIPTION:{Escapar(mensaje)}",
        "END:VALARM"
    ];

    /// <summary>
    /// La fecha del turno con su hora ("HH:mm"), en hora de Argentina. Null si
    /// la hora no vino o no se entiende.
    /// </summary>
    private static DateTime? HoraArgentina(DateTime fecha, string? hora)
    {
        if (!TimeSpan.TryParseExact(hora, @"hh\:mm", CultureInfo.InvariantCulture, out var horaDelDia))
            return null;

        return fecha.Date + horaDelDia;
    }

    /// <summary>
    /// De hora de Argentina a UTC. Se manda en UTC (con la Z al final) para no
    /// tener que describir la zona horaria dentro del archivo: cada calendario
    /// lo muestra después en la hora del teléfono.
    /// </summary>
    private static DateTime AUtc(DateTime horaArgentina) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(horaArgentina, DateTimeKind.Unspecified), FechaArgentina.Zona);

    private static string EnUtc(DateTime utc) =>
        utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    /// <summary>
    /// En los textos, la barra, la coma, el punto y coma y el salto de línea
    /// tienen un significado especial: se escriben con una barra adelante.
    /// </summary>
    public static string Escapar(string texto) =>
        texto
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");

    /// <summary>
    /// Corta una línea larga en pedazos de hasta 75 bytes. Los pedazos que
    /// siguen empiezan con un espacio, que el calendario saca al juntarlos. Se
    /// cuentan bytes y no letras porque una "ñ" o una "á" ocupan dos, y nunca
    /// se corta una letra por la mitad.
    /// </summary>
    public static IEnumerable<string> Cortar(string linea)
    {
        var pedazo = new StringBuilder();
        var bytes = 0;

        // EnumerateRunes recorre letra por letra aunque una ocupe dos char
        // (un emoji, por ejemplo), y Utf8SequenceLength dice cuántos bytes ocupa.
        foreach (var letra in linea.EnumerateRunes())
        {
            var largo = letra.Utf8SequenceLength;
            if (bytes + largo > LargoMaximo)
            {
                yield return pedazo.ToString();
                pedazo.Clear().Append(' ');
                bytes = 1;
            }

            pedazo.Append(letra.ToString());
            bytes += largo;
        }

        yield return pedazo.ToString();
    }
}
