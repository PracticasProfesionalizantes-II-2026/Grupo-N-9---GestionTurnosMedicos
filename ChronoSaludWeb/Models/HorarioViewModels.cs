using System.ComponentModel.DataAnnotations;
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
    // La pantalla de edición usa este mismo orden para sus filas.
    internal static readonly (int Numero, string Nombre)[] Semana =
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

/// <summary>
/// Una fila de la pantalla de edición del horario. No lleva el número de día:
/// sale de la posición de la fila (ver <see cref="HorarioEditarViewModel.Dias"/>).
/// </summary>
public class HorarioDiaEditarViewModel
{
    public bool Atiende { get; set; }

    /// <summary>"HH:mm", como lo postea un input type="time".</summary>
    public string? Desde { get; set; }

    /// <summary>"HH:mm", como lo postea un input type="time".</summary>
    public string? Hasta { get; set; }
}

/// <summary>
/// Pantalla para cargar o editar el horario semanal de un doctor: siete filas
/// fijas, de lunes a domingo, que se guardan todas juntas.
/// </summary>
public class HorarioEditarViewModel : IValidatableObject
{
    /// <summary>Horario estándar que precarga "Cargar horario estándar": de lunes a viernes.</summary>
    public const string EstandarDesde = "08:00";
    public const string EstandarHasta = "14:00";

    public static int DiasPorSemana => HorarioSemanalViewModel.Semana.Length;

    /// <summary>
    /// Una fila por día, en el orden de la semana (lunes primero). El día de
    /// cada fila es su posición y no un campo del formulario, así no se puede
    /// cambiar desde el navegador.
    /// </summary>
    public List<HorarioDiaEditarViewModel> Dias { get; set; } = new();

    /// <summary>
    /// Lo manda solo el botón "Sí, dejar sin horario": guardar los siete días
    /// destildados pide esa confirmación aparte.
    /// </summary>
    public bool ConfirmaSinHorario { get; set; }

    // Lo que sigue lo completa el controlador en cada render. No se postea.
    public int IdDoctor { get; set; }
    public string? NombreDoctor { get; set; }

    /// <summary>
    /// Quien edita es el propio doctor: los textos de la pantalla le hablan
    /// de "tu horario" en vez de nombrar al doctor.
    /// </summary>
    public bool EsPropio { get; set; }

    /// <summary>
    /// Se quiso guardar sin ningún día marcado: la vista muestra el aviso y el
    /// botón para confirmarlo.
    /// </summary>
    public bool PideConfirmarSinHorario { get; set; }

    /// <summary>
    /// Las filas son el horario estándar recién precargado, todavía sin
    /// guardar: la vista lo avisa.
    /// </summary>
    public bool EsEstandar { get; set; }

    /// <summary>
    /// Turnos reservados que quedarían fuera del horario que se quiso guardar.
    /// Si hay alguno, el horario no se guardó.
    /// </summary>
    public IReadOnlyList<TurnoFilaViewModel> Conflictos { get; set; } = Array.Empty<TurnoFilaViewModel>();

    public static string NombreDeDia(int posicion) => HorarioSemanalViewModel.Semana[posicion].Nombre;

    /// <summary>Las siete filas del horario estándar: lunes a viernes, sábado y domingo sin atención.</summary>
    public static List<HorarioDiaEditarViewModel> DiasEstandar() =>
        DiasDesde(Enumerable.Range((int)DayOfWeek.Monday, 5)
            .Select(dia => new HorarioLaboral(dia, EstandarDesde, EstandarHasta)));

    /// <summary>Las siete filas a partir del horario que devuelve la API.</summary>
    public static List<HorarioDiaEditarViewModel> DiasDesde(IEnumerable<HorarioLaboral> horarios)
    {
        var porDia = horarios
            .GroupBy(h => h.DiaSemana)
            .ToDictionary(g => g.Key, g => g.First());

        return HorarioSemanalViewModel.Semana
            .Select(d => porDia.TryGetValue(d.Numero, out var h)
                ? new HorarioDiaEditarViewModel { Atiende = true, Desde = h.HoraInicio, Hasta = h.HoraFin }
                : new HorarioDiaEditarViewModel())
            .ToList();
    }

    /// <summary>
    /// Los días marcados, como los espera la API. Un día sin marcar no se
    /// manda: así es como queda en "No atiende". Usar con el modelo ya validado.
    /// </summary>
    public IReadOnlyList<HorarioLaboral> AHorarios()
    {
        var horarios = new List<HorarioLaboral>();

        for (var i = 0; i < Dias.Count; i++)
        {
            if (Dias[i].Atiende)
                horarios.Add(new HorarioLaboral(HorarioSemanalViewModel.Semana[i].Numero, Dias[i].Desde!, Dias[i].Hasta!));
        }

        return horarios;
    }

    /// <summary>
    /// Solo se validan los días marcados: las horas de un día sin marcar se
    /// ignoran. La API controla que el fin sea posterior al inicio, pero no
    /// que las horas caigan en punto o y media; un horario que no lo hace deja
    /// minutos que nadie puede reservar.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        for (var i = 0; i < Dias.Count; i++)
        {
            var dia = Dias[i];
            if (!dia.Atiende) continue;

            var claveDesde = $"{nameof(Dias)}[{i}].{nameof(HorarioDiaEditarViewModel.Desde)}";
            var claveHasta = $"{nameof(Dias)}[{i}].{nameof(HorarioDiaEditarViewModel.Hasta)}";

            var errorDesde = ErrorDeHora(dia.Desde, "Indicá la hora de inicio.", out var desde);
            var errorHasta = ErrorDeHora(dia.Hasta, "Indicá la hora de fin.", out var hasta);

            if (errorDesde is not null)
                yield return new ValidationResult(errorDesde, new[] { claveDesde });

            if (errorHasta is not null)
                yield return new ValidationResult(errorHasta, new[] { claveHasta });

            if (errorDesde is null && errorHasta is null && hasta <= desde)
            {
                yield return new ValidationResult(
                    "La hora de fin tiene que ser posterior a la de inicio.",
                    new[] { claveHasta });
            }
        }
    }

    /// <summary>Null si la hora sirve; si no, el mensaje para mostrar bajo el campo.</summary>
    private static string? ErrorDeHora(string? texto, string mensajeVacia, out TimeOnly hora)
    {
        hora = default;

        if (string.IsNullOrWhiteSpace(texto))
            return mensajeVacia;

        if (!TimeOnly.TryParseExact(texto, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out hora))
            return "Usá el formato HH:MM.";

        return hora.Minute % HorarioService.MinutosPorFranja == 0
            ? null
            : "Tiene que ser en punto o y media (por ejemplo, 08:00 u 08:30).";
    }
}
