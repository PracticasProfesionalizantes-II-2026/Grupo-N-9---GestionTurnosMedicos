using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Pantalla de pedir turno, en tres pasos que se eligen con enlaces:
///   1. la especialidad;
///   2. el doctor, o "Cualquiera" (cualquier doctor de la especialidad);
///   3. el día, en una tira de dos semanas, y el horario.
/// Lo elegido viaja en la URL (especialidad, idDoctor, fecha), así el flujo no
/// depende de JavaScript ni de la sesión. Tocar un horario lleva a la pantalla
/// de confirmación (<see cref="TurnoConfirmarViewModel"/>).
/// </summary>
public class TurnoCrearViewModel
{
    /// <summary>En la URL, idDoctor=0 quiere decir "cualquier doctor de la especialidad".</summary>
    public const int Cualquiera = 0;

    /// <summary>Cuántos días muestra la tira.</summary>
    public const int DiasDeLaTira = 14;

    public string? Rol { get; set; }

    /// <summary>Paciente ya elegido (se llega desde su ficha). Solo lo usa el personal.</summary>
    public int? IdPaciente { get; set; }

    public IReadOnlyList<string> Especialidades { get; set; } = Array.Empty<string>();
    public string? Especialidad { get; set; }

    /// <summary>Los doctores activos de la especialidad elegida.</summary>
    public IReadOnlyList<DoctorLista> Doctores { get; set; } = Array.Empty<DoctorLista>();

    /// <summary>Null: todavía no se eligió. <see cref="Cualquiera"/>: cualquier doctor de la especialidad.</summary>
    public int? IdDoctor { get; set; }

    /// <summary>Horario semanal del doctor elegido; null con "Cualquiera".</summary>
    public HorarioSemanalViewModel? HorarioDoctor { get; set; }

    public TiraDeDiasViewModel? Tira { get; set; }

    /// <summary>El día elegido en la tira.</summary>
    public DateTime? Fecha { get; set; }

    /// <summary>Horarios libres del día elegido, ordenados por hora.</summary>
    public IReadOnlyList<FranjaViewModel> Franjas { get; set; } = Array.Empty<FranjaViewModel>();

    /// <summary>Error al buscar los días o los horarios, para mostrarlo en su lugar.</summary>
    public string? AvisoFranjas { get; set; }

    /// <summary>En qué paso está: 1 (especialidad), 2 (doctor) o 3 (día y horario).</summary>
    public int Paso
    {
        get
        {
            if (Especialidad is null) return 1;
            if (IdDoctor is null) return 2;
            return 3;
        }
    }

    public bool CualquierDoctor => IdDoctor == Cualquiera;

    /// <summary>El nombre del doctor elegido, o null con "Cualquiera".</summary>
    public string? DoctorNombre => Doctores.FirstOrDefault(d => d.IdDoctor == IdDoctor)?.Nombre.Trim();

    /// <summary>Por ejemplo "jueves 9 de octubre".</summary>
    public string? FechaLarga => Fecha?.ToString("dddd d 'de' MMMM", TurnosIndexViewModel.Cultura);

    /// <summary>
    /// Los parámetros de un enlace a esta misma pantalla. Conserva el paciente
    /// elegido, si lo hay, y lleva lo que se le pasa; lo que va en null no viaja.
    /// </summary>
    public IDictionary<string, string> Ruta(
        string? especialidad, int? idDoctor = null, DateTime? fecha = null, DateTime? desde = null)
    {
        var ruta = new Dictionary<string, string>();

        if (IdPaciente is { } paciente) ruta["paciente"] = $"{paciente}";
        if (especialidad is not null) ruta["especialidad"] = especialidad;
        if (idDoctor is { } doctor) ruta["idDoctor"] = $"{doctor}";
        if (fecha is { } dia) ruta["fecha"] = dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (desde is { } inicio) ruta["desde"] = inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return ruta;
    }

    /// <summary>Los parámetros del enlace de un horario a la pantalla de confirmación.</summary>
    public IDictionary<string, string> RutaConfirmar(FranjaViewModel franja)
    {
        var ruta = new Dictionary<string, string>
        {
            ["idDoctor"] = $"{franja.IdDoctor}",
            ["inicio"] = franja.HoraInicio,
            ["fin"] = franja.HoraFin
        };

        if (Fecha is { } dia) ruta["fecha"] = dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (Especialidad is not null) ruta["especialidad"] = Especialidad;
        if (IdPaciente is { } paciente) ruta["paciente"] = $"{paciente}";

        // Para "Cambiar horario": volver con el mismo doctor o con "Cualquiera".
        if (IdDoctor is { } elegido) ruta["elegido"] = $"{elegido}";

        return ruta;
    }
}

/// <summary>
/// Pantalla de confirmación del turno: el resumen del horario elegido, las
/// observaciones y, para el personal, el paciente. Recién al confirmar se
/// crea el turno.
/// </summary>
public class TurnoConfirmarViewModel : IValidatableObject
{
    private const string FormatoHora = @"^([01]\d|2[0-3]):[0-5]\d$";

    // Lo que se postea. El horario va en campos ocultos: lo eligió la
    // pantalla anterior. Que siga libre lo controla la API al crear.
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un horario de la lista.")]
    public int IdDoctor { get; set; }

    [Required(ErrorMessage = "Elegí un horario de la lista.")]
    public DateTime? Fecha { get; set; }

    [Required(ErrorMessage = "Elegí un horario de la lista.")]
    [RegularExpression(FormatoHora, ErrorMessage = "Elegí un horario de la lista.")]
    public string HoraInicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí un horario de la lista.")]
    [RegularExpression(FormatoHora, ErrorMessage = "Elegí un horario de la lista.")]
    public string HoraFin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí un paciente.")]
    [Display(Name = "Paciente")]
    public int? IdPaciente { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    // Para "Cambiar horario": lo que se había elegido en la pantalla anterior.
    public string? Especialidad { get; set; }
    public int? DoctorElegido { get; set; }

    // Solo para mostrar. No se postean: el controlador los recarga.
    public string? Rol { get; set; }
    public string? DoctorNombre { get; set; }
    public string? DoctorEspecialidad { get; set; }
    public string? Consultorio { get; set; }
    public ElegirPacienteViewModel? ElegirPaciente { get; set; }

    /// <summary>El paciente no elige a nadie: el turno es siempre a su nombre.</summary>
    public bool MostrarSelectorPaciente => Rol != "paciente";

    /// <summary>
    /// Las observaciones y "Confirmar turno" aparecen recién cuando ya se
    /// sabe para quién es el turno.
    /// </summary>
    public bool MostrarFormulario => !MostrarSelectorPaciente || IdPaciente is not null;

    /// <summary>
    /// Esta misma pantalla con el horario elegido: el buscador de pacientes
    /// vuelve acá sin perderlo.
    /// </summary>
    public IDictionary<string, string> RutaConfirmar
    {
        get
        {
            var ruta = new Dictionary<string, string>
            {
                ["idDoctor"] = $"{IdDoctor}",
                ["inicio"] = HoraInicio,
                ["fin"] = HoraFin
            };

            if (FechaIso is not null) ruta["fecha"] = FechaIso;
            if (!string.IsNullOrWhiteSpace(Especialidad)) ruta["especialidad"] = Especialidad;
            if (DoctorElegido is { } doctor) ruta["elegido"] = $"{doctor}";

            return ruta;
        }
    }

    public string DoctorMostrado =>
        string.IsNullOrWhiteSpace(DoctorNombre) ? $"Doctor #{IdDoctor}" : DoctorNombre.Trim();

    /// <summary>Por ejemplo "jueves 9 de octubre de 2026".</summary>
    public string FechaLarga =>
        Fecha?.ToString("dddd d 'de' MMMM 'de' yyyy", TurnosIndexViewModel.Cultura) ?? string.Empty;

    public string Horario => $"{HoraInicio} a {HoraFin}";

    /// <summary>La fecha como la espera el campo oculto.</summary>
    public string? FechaIso => Fecha?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// "Cambiar horario": vuelve a la tira con la misma especialidad, el mismo
    /// doctor (o "Cualquiera") y el mismo día.
    /// </summary>
    public IDictionary<string, string> RutaCambiarHorario
    {
        get
        {
            var ruta = new Dictionary<string, string>();

            if (Rol != "paciente" && IdPaciente is { } paciente) ruta["paciente"] = $"{paciente}";
            if (!string.IsNullOrWhiteSpace(Especialidad)) ruta["especialidad"] = Especialidad;
            if (DoctorElegido is { } doctor) ruta["idDoctor"] = $"{doctor}";
            if (FechaIso is not null) ruta["fecha"] = FechaIso;

            return ruta;
        }
    }

    /// <summary>
    /// True si el horario tiene el formato "HH:mm" y el fin es posterior al
    /// inicio. Lo usa también el controlador para revisar el enlace que llegó.
    /// </summary>
    public static bool HorarioValido(string? inicio, string? fin)
    {
        if (!TimeOnly.TryParseExact(inicio, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var desde))
            return false;
        if (!TimeOnly.TryParseExact(fin, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hasta))
            return false;

        return hasta > desde;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        if (!HorarioValido(HoraInicio, HoraFin))
            yield return new ValidationResult("Elegí un horario de la lista.");
    }
}
