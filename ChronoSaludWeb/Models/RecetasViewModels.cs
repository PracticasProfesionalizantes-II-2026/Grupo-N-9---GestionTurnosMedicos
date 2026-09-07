using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChronoSaludWeb.Models;

public class RecetaMedicamentoViewModel
{
    public int IdMedicamento { get; init; }

    /// <summary>
    /// Resuelto contra GET /medicamentos: la receta solo trae el id.
    /// </summary>
    public string? Nombre { get; init; }

    public string Dosis { get; init; } = string.Empty;
    public string Frecuencia { get; init; } = string.Empty;
    public string? Duracion { get; init; }
    public string? Indicaciones { get; init; }

    public string NombreMostrado =>
        string.IsNullOrWhiteSpace(Nombre) ? $"Medicamento #{IdMedicamento}" : Nombre.Trim();
}

public class RecetaFilaViewModel
{
    public int IdReceta { get; init; }
    public DateTime Fecha { get; init; }
    public DateTime Vigencia { get; init; }
    public string? Detalles { get; init; }
    public IReadOnlyList<RecetaMedicamentoViewModel> Medicamentos { get; init; }
        = Array.Empty<RecetaMedicamentoViewModel>();

    public bool Vigente => Vigencia.Date >= DateTime.Today;

    public string FechaCorta => Fecha.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);
    public string VigenciaCorta => Vigencia.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);
    public string FechaLarga => Fecha.ToString("D", TurnosIndexViewModel.Cultura);
    public string VigenciaLarga => Vigencia.ToString("D", TurnosIndexViewModel.Cultura);

    public bool TieneDetalles => !string.IsNullOrWhiteSpace(Detalles);
}

public class RecetasIndexViewModel
{
    public IReadOnlyList<RecetaFilaViewModel> Recetas { get; init; } = Array.Empty<RecetaFilaViewModel>();

    /// <summary>Paciente cuyas recetas se están mirando.</summary>
    public int? IdPaciente { get; init; }
    public string? NombrePaciente { get; init; }

    /// <summary>Doctor y administrador eligen paciente; el paciente ve el suyo.</summary>
    public bool PuedeElegirPaciente { get; init; }
    public IReadOnlyList<SelectListItem> Pacientes { get; init; } = Array.Empty<SelectListItem>();

    public bool PuedeCrear { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public string? Aviso { get; init; }

    /// <summary>Todavía no se eligió a quién mirarle las recetas.</summary>
    public bool SinPacienteElegido => IdPaciente is null && Aviso is null && !HuboError;

    public int Vigentes => Recetas.Count(r => r.Vigente);
}

public class RecetaDetalleViewModel
{
    public RecetaFilaViewModel? Receta { get; init; }
    public int IdPaciente { get; init; }
    public string? NombrePaciente { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;
}

/// <summary>
/// Una fila del formulario de emisión. Las filas vacías se descartan.
/// </summary>
public class RecetaMedicamentoCampoViewModel
{
    [Display(Name = "Medicamento")]
    public int? IdMedicamento { get; set; }

    [StringLength(100, ErrorMessage = "La dosis no puede superar los 100 caracteres.")]
    [Display(Name = "Dosis")]
    public string? Dosis { get; set; }

    [StringLength(100, ErrorMessage = "La frecuencia no puede superar los 100 caracteres.")]
    [Display(Name = "Frecuencia")]
    public string? Frecuencia { get; set; }

    [StringLength(100, ErrorMessage = "La duración no puede superar los 100 caracteres.")]
    [Display(Name = "Duración")]
    public string? Duracion { get; set; }

    [StringLength(300, ErrorMessage = "Las indicaciones no pueden superar los 300 caracteres.")]
    [Display(Name = "Indicaciones")]
    public string? Indicaciones { get; set; }

    public bool EstaVacia =>
        IdMedicamento is null &&
        string.IsNullOrWhiteSpace(Dosis) &&
        string.IsNullOrWhiteSpace(Frecuencia) &&
        string.IsNullOrWhiteSpace(Duracion) &&
        string.IsNullOrWhiteSpace(Indicaciones);
}

public class RecetaCrearViewModel : IValidatableObject
{
    /// <summary>Cuántas filas de medicamento muestra el formulario.</summary>
    public const int FilasDeMedicamentos = 3;

    [Required(ErrorMessage = "Elegí un paciente.")]
    [Display(Name = "Paciente")]
    public int? IdPaciente { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de emisión")]
    public DateTime? Fecha { get; set; }

    [Required(ErrorMessage = "La fecha de vigencia es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Vigente hasta")]
    public DateTime? Vigencia { get; set; }

    [StringLength(500, ErrorMessage = "Los detalles no pueden superar los 500 caracteres.")]
    [Display(Name = "Detalles")]
    public string? Detalles { get; set; }

    public List<RecetaMedicamentoCampoViewModel> Medicamentos { get; set; } = new();

    // Opciones de los selects. No se postean: el controlador las recarga.
    public IReadOnlyList<SelectListItem> Pacientes { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Vademecum { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>Filas efectivamente cargadas, en el orden del formulario.</summary>
    public IEnumerable<RecetaMedicamentoCampoViewModel> MedicamentosCargados =>
        Medicamentos.Where(m => !m.EstaVacia);

    public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
    {
        if (Fecha is { } emision && Vigencia is { } vence && vence.Date < emision.Date)
        {
            yield return new ValidationResult(
                "La vigencia no puede ser anterior a la fecha de emisión.",
                new[] { nameof(Vigencia) });
        }

        var cargados = MedicamentosCargados.ToList();

        if (cargados.Count == 0)
        {
            yield return new ValidationResult(
                "La receta tiene que incluir al menos un medicamento.",
                new[] { $"{nameof(Medicamentos)}[0].{nameof(RecetaMedicamentoCampoViewModel.IdMedicamento)}" });
            yield break;
        }

        // Una fila empezada a medias es un error: la API exige dosis y frecuencia.
        for (var i = 0; i < Medicamentos.Count; i++)
        {
            var fila = Medicamentos[i];
            if (fila.EstaVacia) continue;

            if (fila.IdMedicamento is null)
            {
                yield return new ValidationResult(
                    "Elegí el medicamento o dejá la fila vacía.",
                    new[] { $"{nameof(Medicamentos)}[{i}].{nameof(RecetaMedicamentoCampoViewModel.IdMedicamento)}" });
            }

            if (string.IsNullOrWhiteSpace(fila.Dosis))
            {
                yield return new ValidationResult(
                    "La dosis es obligatoria.",
                    new[] { $"{nameof(Medicamentos)}[{i}].{nameof(RecetaMedicamentoCampoViewModel.Dosis)}" });
            }

            if (string.IsNullOrWhiteSpace(fila.Frecuencia))
            {
                yield return new ValidationResult(
                    "La frecuencia es obligatoria.",
                    new[] { $"{nameof(Medicamentos)}[{i}].{nameof(RecetaMedicamentoCampoViewModel.Frecuencia)}" });
            }
        }

        if (cargados.Where(m => m.IdMedicamento is not null)
                    .GroupBy(m => m.IdMedicamento)
                    .Any(g => g.Count() > 1))
        {
            // Sin member name: va al resumen de arriba del formulario, porque
            // no corresponde a una sola fila.
            yield return new ValidationResult("Hay un medicamento repetido en la receta.");
        }
    }
}
