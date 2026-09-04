using System.ComponentModel.DataAnnotations;
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

    // Opciones de los selects. No se postean: el controlador las recarga en
    // cada render, incluso cuando la validación falla.
    public IReadOnlyList<SelectListItem> Pacientes { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Doctores { get; set; } = Array.Empty<SelectListItem>();

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
