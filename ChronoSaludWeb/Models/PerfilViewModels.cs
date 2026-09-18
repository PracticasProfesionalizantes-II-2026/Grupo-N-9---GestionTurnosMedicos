using System.ComponentModel.DataAnnotations;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Ficha clínica del propio paciente. Espeja PacienteUpdateDto de la API:
/// todos los campos son opcionales, así que no hay validaciones [Required].
/// </summary>
public class CompletarPerfilPacienteViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de nacimiento")]
    public DateTime? FechaNacimiento { get; set; }

    [Display(Name = "Sexo")]
    public string? Sexo { get; set; }

    [Display(Name = "Grupo sanguíneo")]
    public string? GrupoSanguineo { get; set; }

    [StringLength(500, ErrorMessage = "Las alergias no pueden superar los 500 caracteres.")]
    [Display(Name = "Alergias")]
    public string? Alergias { get; set; }

    [StringLength(500, ErrorMessage = "Las condiciones no pueden superar los 500 caracteres.")]
    [Display(Name = "Condiciones preexistentes")]
    public string? Condiciones { get; set; }

    public string? Error { get; set; }
    public bool HuboError => Error is not null;
}
