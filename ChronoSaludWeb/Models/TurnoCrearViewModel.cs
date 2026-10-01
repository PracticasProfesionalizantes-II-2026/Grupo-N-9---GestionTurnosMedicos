using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChronoSaludWeb.Models;

public class TurnoCrearViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Elegí un paciente.")]
    [Display(Name = "Paciente")]
    public int? IdPaciente { get; set; }

    [Required(ErrorMessage = "Elegí un doctor.")]
    [Display(Name = "Doctor")]
    public int? IdDoctor { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime? FechaInicio { get; set; }

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Usá el formato HH:MM.")]
    [Display(Name = "Hora de inicio")]
    public string HoraInicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Usá el formato HH:MM.")]
    [Display(Name = "Hora de fin")]
    public string HoraFin { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    // Filtros de la búsqueda de horarios. En el GET llegan por query string
    // (especialidad, idDoctor, fecha) y en el POST viajan ocultos, para poder
    // recargar las mismas franjas si la API rechaza el turno.
    [Display(Name = "Especialidad")]
    public string? Especialidad { get; set; }

    /// <summary>Doctor elegido en el filtro. Null = "Cualquiera".</summary>
    [Display(Name = "Doctor")]
    public int? DoctorElegido { get; set; }

    /// <summary>
    /// Lo postea el botón de la franja elegida ("idDoctor|HH:mm|HH:mm"). El
    /// controlador lo desarma en IdDoctor, HoraInicio y HoraFin.
    /// </summary>
    public string? Franja { get; set; }

    // Opciones de los selects. No se postean: el controlador las recarga en
    // cada render, incluso cuando la validación falla.
    public IReadOnlyList<SelectListItem> Pacientes { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>Doctores de la especialidad elegida (sin la opción "Cualquiera").</summary>
    public IReadOnlyList<SelectListItem> Doctores { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Especialidades { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>Horarios libres para los filtros actuales, ordenados por hora.</summary>
    public IReadOnlyList<FranjaViewModel> Franjas { get; set; } = Array.Empty<FranjaViewModel>();

    /// <summary>Horario semanal del doctor elegido; null con "Cualquiera".</summary>
    public HorarioSemanalViewModel? HorarioDoctor { get; set; }

    /// <summary>Error al buscar las franjas, para mostrarlo en lugar de la grilla.</summary>
    public string? AvisoFranjas { get; set; }

    /// <summary>
    /// "Hoy" según el reloj local del servidor, igual que el resto del front y
    /// que la API. Se toma una sola vez para que la fecha por defecto, el
    /// recorte de fechas pasadas y el min del input usen el mismo valor.
    /// </summary>
    public DateTime Hoy { get; } = DateTime.Today;

    public string FechaMinima => Hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>La fecha elegida en el formato que esperan input type="date" y el campo oculto.</summary>
    public string? FechaIso => FechaInicio?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public bool HayEspecialidad => !string.IsNullOrWhiteSpace(Especialidad);

    public bool BuscaCualquiera => DoctorElegido is null;

    /// <summary>Por ejemplo "jueves 2 de octubre".</summary>
    public string? FechaLarga =>
        FechaInicio?.ToString("dddd d 'de' MMMM", TurnosIndexViewModel.Cultura);

    /// <summary>Lo fija el controlador; define si se muestra el selector de paciente.</summary>
    public string? Rol { get; set; }

    /// <summary>
    /// Un paciente no elige de una lista: el controlador ya le fijó su propio
    /// IdPaciente antes de renderizar, así que el selector sobra.
    /// </summary>
    public bool MostrarSelectorPaciente => Rol != "paciente";

    /// <summary>
    /// La API no valida que el fin sea posterior al inicio, así que lo cortamos acá
    /// para no cargar turnos con un rango imposible.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        if (TimeOnly.TryParse(HoraInicio, out var inicio) &&
            TimeOnly.TryParse(HoraFin, out var fin) &&
            fin <= inicio)
        {
            yield return new ValidationResult(
                "La hora de fin tiene que ser posterior a la de inicio.",
                new[] { nameof(HoraFin) });
        }
    }
}
