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

    /// <summary>El resultado tiene un PDF subido a la app (paso 23).</summary>
    public bool TienePdf { get; init; }
    public string? NombrePdf { get; init; }

    /// <summary>Lo mira el paciente: el estado se dice desde su lado.</summary>
    public bool ParaPaciente { get; init; }

    /// <summary>"Análisis de sangre". Un tipo desconocido se muestra tal cual.</summary>
    public string TipoTexto => NombreDeTipo(Tipo);

    /// <summary>Qué estudio es; si vino sin descripción, el tipo.</summary>
    public string Titulo => string.IsNullOrWhiteSpace(Descripcion) ? TipoTexto : Descripcion.Trim();

    /// <summary>"pendiente" es que el resultado todavía no está; los otros estados, que sí.</summary>
    public bool TieneResultado => !string.Equals(Estado, "pendiente", StringComparison.OrdinalIgnoreCase);

    /// <summary>Hay resultado y el paciente todavía no lo vio ("validado").</summary>
    public bool SinVer => string.Equals(Estado, "validado", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// El estado dicho en palabras simples. Al personal le importa si el
    /// paciente ya lo vio; al paciente, si es nuevo. Cualquier otro estado
    /// con resultado se muestra como visto.
    /// </summary>
    public string EstadoTexto => !TieneResultado ? "Esperando resultado"
        : ParaPaciente ? (SinVer ? "Nuevo resultado" : "Resultado disponible")
        : SinVer ? "Sin ver por el paciente" : "Visto por el paciente";

    public TonoChip Tono => !TieneResultado ? TonoChip.Aviso
        : ParaPaciente ? (SinVer ? TonoChip.Exito : TonoChip.Neutro)
        : SinVer ? TonoChip.Info : TonoChip.Exito;

    /// <summary>Si entra en el filtro de estado elegido (ver <see cref="FiltroDeEstudios"/>).</summary>
    public bool Coincide(string? tipo, string? estado) =>
        (string.IsNullOrEmpty(tipo) || Tipo == tipo)
        && estado switch
        {
            FiltroDeEstudios.Esperando => !TieneResultado,
            FiltroDeEstudios.ConResultado => TieneResultado,
            FiltroDeEstudios.SinVer => SinVer,
            FiltroDeEstudios.Visto => TieneResultado && !SinVer,
            _ => true
        };

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

    public static EstudioFilaViewModel Desde(Estudio estudio, bool paraPaciente = false) => new()
    {
        ParaPaciente = paraPaciente,
        TienePdf = estudio.TienePdf,
        NombrePdf = estudio.NombrePdf,
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

/// <summary>
/// Los filtros de estado de Estudios/Index. «Con resultado» junta validado y
/// entregado, así que se filtra en la Web: la lista de un paciente es corta.
/// </summary>
public static class FiltroDeEstudios
{
    public const string Esperando = "esperando";
    public const string ConResultado = "con-resultado";
    public const string SinVer = "sin-ver";
    public const string Visto = "visto";

    /// <summary>Lo que se ofrece a cada uno: al paciente no le sirve "sin ver".</summary>
    public static IReadOnlyList<(string Valor, string Nombre)> Opciones(bool paraPaciente) => paraPaciente
        ? new[] { (Esperando, "Esperando resultado"), (ConResultado, "Con resultado") }
        : new[] { (Esperando, "Esperando resultado"), (ConResultado, "Con resultado"), (SinVer, "Sin ver por el paciente"), (Visto, "Visto por el paciente") };

    /// <summary>Lo que llega por la URL y no es una opción queda en "todos".</summary>
    public static string? Validar(string? estado, bool paraPaciente) =>
        Opciones(paraPaciente).Any(o => o.Valor == estado) ? estado : null;
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

    /// <summary>Los filtros elegidos (paso 23).</summary>
    public string? Tipo { get; init; }
    public string? FiltroEstado { get; init; }

    /// <summary>Cuántos estudios tiene el paciente, antes de filtrar.</summary>
    public int TotalSinFiltrar { get; init; }

    public bool HayFiltro => Tipo is not null || FiltroEstado is not null;

    /// <summary>Hay estudios, pero ninguno entra en el filtro.</summary>
    public bool NingunoCoincide => HayFiltro && Estudios.Count == 0 && TotalSinFiltrar > 0;

    /// <summary>Cuántos ve el paciente por primera vez en esta visita.</summary>
    public int Nuevos => EsPaciente ? Estudios.Count(e => e.SinVer) : 0;
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

    /// <summary>El PDF del resultado (paso 23). Reemplaza al que hubiera.</summary>
    [Display(Name = "Archivo PDF (opcional)")]
    public IFormFile? Pdf { get; set; }

    /// <summary>Ya tenía PDF y se lo quiere sacar.</summary>
    [Display(Name = "Quitar el PDF que ya tiene")]
    public bool QuitarPdf { get; set; }

    /// <summary>Lo mismo que acepta la API.</summary>
    public const int TamanoMaximoPdf = 5 * 1024 * 1024;

    /// <summary>
    /// Qué tiene de malo el PDF elegido; null si está bien. Se revisa la
    /// firma del archivo ("%PDF-"), no la extensión: la API hace lo mismo.
    /// </summary>
    public static string? ProblemaDelPdf(ReadOnlySpan<byte> contenido)
    {
        if (contenido.Length == 0) return "El archivo está vacío.";
        if (contenido.Length > TamanoMaximoPdf) return "El PDF puede pesar hasta 5 MB.";
        if (contenido.Length < 5 || !contenido[..5].SequenceEqual("%PDF-"u8)) return "El archivo no es un PDF. Elegí el informe en PDF.";
        return null;
    }

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
