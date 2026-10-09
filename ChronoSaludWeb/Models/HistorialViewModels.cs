using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChronoSaludWeb.Models;

public class EntradaHistorialViewModel
{
    public int IdHistorial { get; init; }
    public DateTime Fecha { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Diagnostico { get; init; } = string.Empty;
    public int? IdTurno { get; init; }

    /// <summary>Quién escribió la entrada. Null si la API no lo mandó.</summary>
    public FirmaViewModel? Firma { get; init; }

    /// <summary>La puede editar quien la mira: es el doctor que la escribió.</summary>
    public bool PuedeEditar { get; init; }

    /// <summary>
    /// El turno vinculado se muestra como enlace solo si quien mira puede
    /// abrirlo; si no, va como texto.
    /// </summary>
    public bool EnlaceAlTurno { get; init; }

    public string FechaLarga => Fecha.ToString("D", TurnosIndexViewModel.Cultura);
    public string FechaCorta => Fecha.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);

    public string DiagnosticoMostrado =>
        string.IsNullOrWhiteSpace(Diagnostico) ? "Sin diagnóstico" : Diagnostico.Trim();

    public bool TieneDescripcion => !string.IsNullOrWhiteSpace(Descripcion);
}

public class HistorialFiltroViewModel
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public bool HayAlguno => Desde is not null || Hasta is not null;

    /// <summary>El rango está al revés y por eso no va a traer nada.</summary>
    public bool RangoInvertido => Desde is not null && Hasta is not null && Desde > Hasta;
}

public class HistorialIndexViewModel
{
    public IReadOnlyList<EntradaHistorialViewModel> Entradas { get; init; }
        = Array.Empty<EntradaHistorialViewModel>();

    public int? IdPaciente { get; init; }
    public string? NombrePaciente { get; init; }

    public HistorialFiltroViewModel Filtros { get; init; } = new();

    /// <summary>Doctor y administrador eligen paciente; el paciente ve el suyo.</summary>
    public bool PuedeElegirPaciente { get; init; }
    public IReadOnlyList<SelectListItem> Pacientes { get; init; } = Array.Empty<SelectListItem>();

    public bool PuedeEscribir { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public string? Aviso { get; init; }

    /// <summary>Todavía no se eligió de quién ver la historia clínica.</summary>
    public bool SinPacienteElegido => IdPaciente is null && Aviso is null && !HuboError;

    public bool SinResultados => Entradas.Count == 0 && Filtros.HayAlguno;
}

/// <summary>
/// El formulario de una entrada: nueva, nueva desde un turno o para editar
/// una que ya existe.
/// </summary>
public class EntradaHistorialCrearViewModel
{
    /// <summary>La entrada que se edita. Null si es una nueva.</summary>
    public int? IdHistorial { get; set; }

    /// <summary>El turno al que queda vinculada. Null si no tiene.</summary>
    public int? IdTurno { get; set; }

    /// <summary>Marcar el turno como completado al guardar la consulta.</summary>
    [Display(Name = "Marcar el turno como completado")]
    public bool CompletarTurno { get; set; }

    [Required(ErrorMessage = "Elegí un paciente.")]
    [Display(Name = "Paciente")]
    public int? IdPaciente { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de la consulta")]
    public DateTime? Fecha { get; set; }

    [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
    [StringLength(200, ErrorMessage = "El diagnóstico no puede superar los 200 caracteres.")]
    [Display(Name = "Diagnóstico")]
    public string Diagnostico { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    [Display(Name = "Descripción de la consulta")]
    public string Descripcion { get; set; } = string.Empty;

    // Opciones del select. No se postea: el controlador la recarga.
    public IReadOnlyList<SelectListItem> Pacientes { get; set; } = Array.Empty<SelectListItem>();

    // Para mostrar. No se postean: el controlador los recarga.
    public string? NombrePaciente { get; set; }
    public TurnoAtendidoViewModel? Turno { get; set; }

    public bool EsEdicion => IdHistorial is not null;

    /// <summary>
    /// Desde un turno o al editar, el paciente ya está decidido: no se
    /// muestra el desplegable.
    /// </summary>
    public bool PacienteFijo => EsEdicion || IdTurno is not null;

    /// <summary>La casilla se muestra en una consulta nueva desde un turno que ya empezó.</summary>
    public bool MostrarCompletarTurno => !EsEdicion && Turno is { PuedeCompletarse: true };
}
