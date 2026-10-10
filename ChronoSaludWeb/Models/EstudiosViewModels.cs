using System.ComponentModel.DataAnnotations;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>Un tipo de estudio: el valor que entiende la API y cómo se lo nombra.</summary>
public record TipoDeEstudio(string Valor, string Nombre);

/// <summary>Un estudio de la lista, listo para mostrar.</summary>
public class EstudioFilaViewModel
{
    /// <summary>Los tipos que acepta la API, en el orden en que se ofrecen.</summary>
    public static readonly TipoDeEstudio[] Tipos =
    [
        new("sangre", "Análisis de sangre"),
        new("imagen", "Estudio por imágenes"),
        new("biopsia", "Biopsia"),
        new("otro", "Otro")
    ];

    public int IdEstudio { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaSolicitud { get; init; }
    public DateTime? FechaResultado { get; init; }
    public string? Resultado { get; init; }
    public string? ArchivoUrl { get; init; }

    /// <summary>Quién lo pidió. Null si el estudio no tiene turno.</summary>
    public FirmaViewModel? PedidoPor { get; init; }

    /// <summary>"Análisis de sangre". Un tipo desconocido se muestra tal cual.</summary>
    public string TipoTexto => NombreDeTipo(Tipo);

    /// <summary>Qué estudio es; si vino sin descripción, el tipo.</summary>
    public string Titulo => string.IsNullOrWhiteSpace(Descripcion) ? TipoTexto : Descripcion.Trim();

    /// <summary>"pendiente" es que el resultado todavía no está; los otros estados, que sí.</summary>
    public bool TieneResultado => !string.Equals(Estado, "pendiente", StringComparison.OrdinalIgnoreCase);

    /// <summary>El estado dicho en palabras simples.</summary>
    public string EstadoTexto => TieneResultado ? "Resultado disponible" : "Esperando resultado";

    public TonoChip Tono => TieneResultado ? TonoChip.Exito : TonoChip.Aviso;

    public string FechaSolicitudTexto => FechaArgentina.Corta(FechaSolicitud);

    public string? FechaResultadoTexto => FechaResultado is { } fecha ? FechaArgentina.Corta(fecha) : null;

    /// <summary>
    /// El enlace al archivo, solo si es una dirección web segura (https). Así
    /// la vista nunca arma un enlace a algo raro que se haya guardado.
    /// </summary>
    public string? ArchivoSeguro => EstudioResultadoViewModel.EsEnlaceValido(ArchivoUrl) ? ArchivoUrl!.Trim() : null;

    public static string NombreDeTipo(string tipo)
    {
        foreach (var opcion in Tipos)
        {
            if (opcion.Valor == tipo)
                return opcion.Nombre;
        }
        return tipo;
    }

    public static EstudioFilaViewModel Desde(Estudio estudio) => new()
    {
        IdEstudio = estudio.IdEstudio,
        Tipo = estudio.Tipo,
        Descripcion = estudio.Descripcion,
        Estado = estudio.Estado,
        FechaSolicitud = estudio.FechaSolicitud,
        FechaResultado = estudio.FechaResultado,
        Resultado = estudio.Resultado,
        ArchivoUrl = estudio.ArchivoUrl,
        PedidoPor = FirmaViewModel.Desde(estudio.Doctor)
    };
}

/// <summary>La pantalla Estudios/Index.</summary>
public class EstudiosIndexViewModel
{
    public IReadOnlyList<EstudioFilaViewModel> Estudios { get; init; } = Array.Empty<EstudioFilaViewModel>();

    /// <summary>El paciente cuyos estudios se están mirando.</summary>
    public int? IdPaciente { get; init; }
    public string? NombrePaciente { get; init; }

    /// <summary>El buscador de pacientes. Solo para doctor y administrador.</summary>
    public ElegirPacienteViewModel? ElegirPaciente { get; init; }

    /// <summary>Doctor y administrador ven "Cargar resultado".</summary>
    public bool PuedeCargarResultados { get; init; }

    /// <summary>Lo ve el paciente: el texto cambia ("tus estudios").</summary>
    public bool EsPaciente { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public string? Aviso { get; init; }

    /// <summary>Todavía no se eligió a quién mirarle los estudios.</summary>
    public bool SinPacienteElegido => IdPaciente is null && Aviso is null && !HuboError;

    public int ConResultado => Estudios.Count(e => e.TieneResultado);
}

/// <summary>Pedir un estudio desde un turno (Estudios/Crear?turno=N). Solo el doctor.</summary>
public class EstudioCrearViewModel
{
    [Required]
    public int? IdTurno { get; set; }

    [Display(Name = "Tipo de estudio")]
    [Required(ErrorMessage = "Elegí el tipo de estudio.")]
    public string? Tipo { get; set; }

    [Display(Name = "Qué estudio")]
    [Required(ErrorMessage = "Escribí qué estudio se pide.")]
    [StringLength(300, ErrorMessage = "La descripción puede tener hasta 300 caracteres.")]
    public string? Descripcion { get; set; }

    // Lo que se muestra y no se postea.
    public TurnoAtendidoViewModel? Turno { get; set; }
    public string? NombrePaciente { get; set; }

    public static bool EsTipoValido(string? tipo) =>
        tipo is not null && EstudioFilaViewModel.Tipos.Any(t => t.Valor == tipo);
}

/// <summary>Cargar el resultado de un estudio (Estudios/Resultado/{id}). Doctor o administrador.</summary>
public class EstudioResultadoViewModel
{
    public int IdEstudio { get; set; }

    /// <summary>A qué paciente vuelve la lista después de guardar.</summary>
    public int IdPaciente { get; set; }

    [Display(Name = "Resultado")]
    [Required(ErrorMessage = "Escribí el resultado.")]
    [StringLength(4000, ErrorMessage = "El resultado puede tener hasta 4000 caracteres.")]
    public string? Resultado { get; set; }

    [Display(Name = "Enlace al archivo (opcional)")]
    [StringLength(500, ErrorMessage = "El enlace puede tener hasta 500 caracteres.")]
    public string? ArchivoUrl { get; set; }

    // Lo que se muestra y no se postea.
    public EstudioFilaViewModel? Estudio { get; set; }
    public string? NombrePaciente { get; set; }

    /// <summary>Ya tenía resultado: el título dice "Corregir".</summary>
    public bool EsCorreccion => Estudio?.TieneResultado == true;

    /// <summary>
    /// True solo si es una dirección completa que empieza con https://. Vacío
    /// da false: el controlador lo acepta aparte, porque el enlace es opcional.
    /// </summary>
    public static bool EsEnlaceValido(string? enlace)
    {
        if (string.IsNullOrWhiteSpace(enlace))
            return false;

        return Uri.TryCreate(enlace.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}
