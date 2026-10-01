using System.Globalization;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Un botón de horario libre en la pantalla de pedir turno. El doctor viaja en
/// la franja porque con "Cualquiera" cada botón puede ser de un doctor distinto.
/// </summary>
public class FranjaViewModel
{
    public int IdDoctor { get; init; }

    /// <summary>Solo se muestra cuando se buscó en "Cualquiera".</summary>
    public string? DoctorNombre { get; init; }

    public string HoraInicio { get; init; } = string.Empty;
    public string HoraFin { get; init; } = string.Empty;

    /// <summary>Lo que postea el botón: "idDoctor|HH:mm|HH:mm".</summary>
    public string Valor => $"{IdDoctor}|{HoraInicio}|{HoraFin}";

    /// <summary>
    /// Desarma lo que postea el botón. El valor llega del navegador, así que se
    /// valida el formato; que la franja sea real lo termina de controlar la API
    /// (400 fuera de horario, 409 si ya está ocupada).
    /// </summary>
    public static bool TryParse(string? valor, out int idDoctor, out string horaInicio, out string horaFin)
    {
        idDoctor = 0;
        horaInicio = horaFin = string.Empty;

        var partes = valor?.Split('|');
        if (partes is not { Length: 3 }) return false;

        if (!int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out idDoctor) || idDoctor <= 0)
            return false;

        if (!TimeOnly.TryParseExact(partes[1], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            !TimeOnly.TryParseExact(partes[2], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return false;

        horaInicio = partes[1];
        horaFin = partes[2];
        return true;
    }
}

/// <summary>Una fila del horario semanal: el día y su rango, o null si no atiende.</summary>
public record DiaHorario(string Dia, string? Rango);

/// <summary>
/// Horario semanal de un doctor listo para mostrar: los siete días, de lunes
/// a domingo, con "No atiende" en los que no tiene horario cargado.
/// </summary>
public class HorarioSemanalViewModel
{
    // La API numera como DayOfWeek (0 = domingo); acá se muestra empezando el lunes.
    private static readonly (int Numero, string Nombre)[] Semana =
    [
        (1, "Lunes"), (2, "Martes"), (3, "Miércoles"), (4, "Jueves"),
        (5, "Viernes"), (6, "Sábado"), (0, "Domingo")
    ];

    public IReadOnlyList<DiaHorario> Dias { get; init; } = Array.Empty<DiaHorario>();

    public bool TieneHorario => Dias.Any(d => d.Rango is not null);

    public static HorarioSemanalViewModel Desde(IEnumerable<HorarioLaboral> horarios)
    {
        var porDia = horarios
            .GroupBy(h => h.DiaSemana)
            .ToDictionary(g => g.Key, g => g.First());

        return new HorarioSemanalViewModel
        {
            Dias = Semana
                .Select(d => new DiaHorario(
                    d.Nombre,
                    porDia.TryGetValue(d.Numero, out var h) ? $"{h.HoraInicio} a {h.HoraFin}" : null))
                .ToList()
        };
    }
}
