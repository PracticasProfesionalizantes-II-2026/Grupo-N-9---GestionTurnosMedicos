using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Formato con el que una fila "Otro..." guarda el nombre escrito a mano dentro
/// de Indicaciones: la primera línea es "Medicamento: nombre" y, si hay
/// indicaciones, van debajo.
/// </summary>
public static class IndicacionesDeOtro
{
    public const string Prefijo = "Medicamento: ";

    public static string Componer(string? nombre, string? indicaciones)
    {
        var primeraLinea = Prefijo + Normalizar(nombre);
        return string.IsNullOrWhiteSpace(indicaciones)
            ? primeraLinea
            : $"{primeraLinea}\n{indicaciones.Trim()}";
    }

    /// <summary>Lo inverso de Componer. Nombre es null si el texto no respeta el formato.</summary>
    public static (string? Nombre, string? Indicaciones) Separar(string? texto)
    {
        if (texto is null || !texto.StartsWith(Prefijo, StringComparison.Ordinal))
            return (null, texto);

        var corte = texto.IndexOf('\n');
        var nombre = (corte < 0 ? texto[Prefijo.Length..] : texto[Prefijo.Length..corte]).Trim();
        if (nombre.Length == 0)
            return (null, texto);

        var resto = corte < 0 ? string.Empty : texto[(corte + 1)..].Trim();
        return (nombre, resto.Length == 0 ? null : resto);
    }

    // El nombre va en una sola línea: es lo que permite volver a separarlo.
    private static string Normalizar(string? nombre) =>
        string.Join(' ', (nombre ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

public class RecetaMedicamentoViewModel
{
    public int IdMedicamento { get; init; }

    /// <summary>
    /// Nombre comercial. Como el genérico, la concentración y la forma, es la
    /// copia que guardó la receta al emitirse.
    /// </summary>
    public string? Nombre { get; init; }

    public string? NombreGenerico { get; init; }
    public string? Concentracion { get; init; }
    public string? FormaFarmaceutica { get; init; }

    /// <summary>La fila apunta al marcador "Otro...": el nombre está en Indicaciones.</summary>
    public bool EsOtro { get; init; }

    public string Dosis { get; init; } = string.Empty;
    public string Frecuencia { get; init; } = string.Empty;
    public string? Duracion { get; init; }
    public string? Indicaciones { get; init; }

    public string NombreMostrado
    {
        get
        {
            if (EsOtro)
                return IndicacionesDeOtro.Separar(Indicaciones).Nombre ?? "Otro medicamento";

            return string.IsNullOrWhiteSpace(Nombre) ? $"Medicamento #{IdMedicamento}" : Nombre.Trim();
        }
    }

    /// <summary>
    /// "NOMBRE COMERCIAL (nombre genérico) concentración · forma farmacéutica".
    /// Lo que falte se omite.
    /// </summary>
    public string DescripcionCompleta
    {
        get
        {
            var descripcion = NombreMostrado;
            if (EsOtro) return descripcion;

            if (!string.IsNullOrWhiteSpace(NombreGenerico))
                descripcion += $" ({NombreGenerico.Trim().ToLower(TurnosIndexViewModel.Cultura)})";

            if (!string.IsNullOrWhiteSpace(Concentracion))
                descripcion += $" {Concentracion.Trim()}";

            if (!string.IsNullOrWhiteSpace(FormaFarmaceutica))
                descripcion += $" · {FormaFarmaceutica.Trim().ToLower(TurnosIndexViewModel.Cultura)}";

            return descripcion;
        }
    }

    /// <summary>
    /// Para la hoja impresa va primero el genérico: "Ibuprofeno 400 mg ·
    /// comprimido (IBUPIRAC)". Sin genérico queda la descripción de siempre.
    /// </summary>
    public string DescripcionParaImprimir
    {
        get
        {
            if (EsOtro || string.IsNullOrWhiteSpace(NombreGenerico))
                return DescripcionCompleta;

            var generico = NombreGenerico.Trim().ToLower(TurnosIndexViewModel.Cultura);
            var descripcion = char.ToUpper(generico[0], TurnosIndexViewModel.Cultura) + generico[1..];

            if (!string.IsNullOrWhiteSpace(Concentracion))
                descripcion += $" {Concentracion.Trim()}";

            if (!string.IsNullOrWhiteSpace(FormaFarmaceutica))
                descripcion += $" · {FormaFarmaceutica.Trim().ToLower(TurnosIndexViewModel.Cultura)}";

            if (!string.IsNullOrWhiteSpace(Nombre))
                descripcion += $" ({Nombre.Trim()})";

            return descripcion;
        }
    }

    /// <summary>Las indicaciones sin la línea del nombre en las filas "Otro...".</summary>
    public string? IndicacionesMostradas =>
        EsOtro ? IndicacionesDeOtro.Separar(Indicaciones).Indicaciones : Indicaciones;

    public static RecetaMedicamentoViewModel Desde(MedicamentoRecetado medicamento) => new()
    {
        IdMedicamento = medicamento.IdMedicamento,
        Nombre = medicamento.Nombre,
        NombreGenerico = medicamento.NombreGenerico,
        Concentracion = medicamento.Concentracion,
        FormaFarmaceutica = medicamento.FormaFarmaceutica,
        EsOtro = MedicamentoService.EsMarcadorOtro(medicamento.Nombre, medicamento.NombreGenerico),
        Dosis = medicamento.Dosis,
        Frecuencia = medicamento.Frecuencia,
        Duracion = medicamento.Duracion,
        Indicaciones = medicamento.Indicaciones
    };
}

public class RecetaFilaViewModel
{
    public int IdReceta { get; init; }
    public DateTime Fecha { get; init; }
    public DateTime Vigencia { get; init; }
    public string? Detalles { get; init; }
    public IReadOnlyList<RecetaMedicamentoViewModel> Medicamentos { get; init; }
        = Array.Empty<RecetaMedicamentoViewModel>();

    /// <summary>Quién la firmó. Null si la API no lo mandó.</summary>
    public FirmaViewModel? Firma { get; init; }

    /// <summary>El turno al que está vinculada, si tiene.</summary>
    public int? IdTurno { get; init; }

    public bool Vigente => Vigencia.Date >= FechaArgentina.Hoy();

    public string FechaCorta => Fecha.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);
    public string VigenciaCorta => Vigencia.ToString("d MMM yyyy", TurnosIndexViewModel.Cultura);
    public string FechaLarga => Fecha.ToString("D", TurnosIndexViewModel.Cultura);
    public string VigenciaLarga => Vigencia.ToString("D", TurnosIndexViewModel.Cultura);

    public bool TieneDetalles => !string.IsNullOrWhiteSpace(Detalles);

    public static RecetaFilaViewModel Desde(Receta receta) => new()
    {
        IdReceta = receta.IdReceta,
        Fecha = receta.Fecha,
        Vigencia = receta.Vigencia,
        Detalles = receta.Detalles,
        Medicamentos = receta.Medicamentos.Select(RecetaMedicamentoViewModel.Desde).ToList(),
        Firma = FirmaViewModel.Desde(receta.Doctor),
        IdTurno = receta.IdTurno
    };
}

public class RecetasIndexViewModel
{
    public IReadOnlyList<RecetaFilaViewModel> Recetas { get; init; } = Array.Empty<RecetaFilaViewModel>();

    /// <summary>Paciente cuyas recetas se están mirando.</summary>
    public int? IdPaciente { get; init; }
    public string? NombrePaciente { get; init; }

    /// <summary>
    /// El buscador de pacientes. Solo para doctor y administrador: el
    /// paciente ve las suyas.
    /// </summary>
    public ElegirPacienteViewModel? ElegirPaciente { get; init; }

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

    /// <summary>La puede editar quien la mira: es el doctor que la firmó.</summary>
    public bool PuedeEditar { get; init; }

    /// <summary>
    /// El turno vinculado se muestra como enlace solo si quien mira puede
    /// abrirlo; si no, va como texto.
    /// </summary>
    public bool EnlaceAlTurno { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;
}

/// <summary>
/// La hoja de la receta para imprimir (Recetas/Imprimir).
/// </summary>
public class RecetaImprimirViewModel
{
    public RecetaFilaViewModel? Receta { get; init; }
    public int IdPaciente { get; init; }
    public string? NombrePaciente { get; init; }
    public string? TipoDocumento { get; init; }
    public string? Dni { get; init; }

    /// <summary>Cuándo se armó la hoja, en hora de Argentina.</summary>
    public DateTime GeneradaEl { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    /// <summary>"DNI 30111222", solo el número si no hay tipo, o null si no hay número.</summary>
    public string? Documento
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Dni)) return null;
            if (string.IsNullOrWhiteSpace(TipoDocumento)) return Dni;
            return $"{TipoDocumento} {Dni}";
        }
    }

    public string GeneradaElTexto => GeneradaEl.ToString("d/M/yyyy, HH:mm", TurnosIndexViewModel.Cultura);
}

/// <summary>
/// Una fila del formulario de emisión. Las filas vacías se descartan.
/// </summary>
public class RecetaMedicamentoCampoViewModel
{
    public const int LargoMaximoDeIndicaciones = 300;

    [Display(Name = "Medicamento")]
    public int? IdMedicamento { get; set; }

    /// <summary>Solo cuenta si se eligió "Otro..."; si no, se ignora.</summary>
    [StringLength(150, ErrorMessage = "El nombre del medicamento no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre del medicamento")]
    public string? NombreOtro { get; set; }

    public bool EsOtro => IdMedicamento == RecetaCrearViewModel.IdOtro;

    [StringLength(100, ErrorMessage = "La dosis no puede superar los 100 caracteres.")]
    [Display(Name = "Dosis")]
    public string? Dosis { get; set; }

    [StringLength(100, ErrorMessage = "La frecuencia no puede superar los 100 caracteres.")]
    [Display(Name = "Frecuencia")]
    public string? Frecuencia { get; set; }

    [StringLength(100, ErrorMessage = "La duración no puede superar los 100 caracteres.")]
    [Display(Name = "Duración")]
    public string? Duracion { get; set; }

    [StringLength(LargoMaximoDeIndicaciones, ErrorMessage = "Las indicaciones no pueden superar los 300 caracteres.")]
    [Display(Name = "Indicaciones")]
    public string? Indicaciones { get; set; }

    public bool EstaVacia =>
        IdMedicamento is null &&
        string.IsNullOrWhiteSpace(Dosis) &&
        string.IsNullOrWhiteSpace(Frecuencia) &&
        string.IsNullOrWhiteSpace(Duracion) &&
        string.IsNullOrWhiteSpace(Indicaciones);
}

/// <summary>
/// El formulario de una receta: nueva, nueva desde un turno o para editar
/// una que ya existe.
/// </summary>
public class RecetaCrearViewModel : IValidatableObject
{
    /// <summary>El formulario arranca con una fila; se agregan y quitan con "+" y "-".</summary>
    public const int MinimoDeFilas = 1;
    public const int MaximoDeFilas = 10;

    /// <summary>
    /// Valor de la opción "Otro..." en el desplegable. No es un id de la API:
    /// el controlador lo cambia por el del medicamento marcador al emitir.
    /// </summary>
    public const int IdOtro = -1;

    /// <summary>La receta que se edita. Null si es una nueva.</summary>
    public int? IdReceta { get; set; }

    /// <summary>El turno al que queda vinculada. Null si no tiene.</summary>
    public int? IdTurno { get; set; }

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

    // Opciones del desplegable de medicamentos. No se postean: el controlador las recarga.
    public IReadOnlyList<SelectListItem> Vademecum { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>El marcador existe en la API: se puede ofrecer "Otro...".</summary>
    public bool OtroDisponible { get; set; }

    // Para mostrar. No se postean: el controlador los recarga.
    public ElegirPacienteViewModel? ElegirPaciente { get; set; }
    public TurnoAtendidoViewModel? Turno { get; set; }

    public bool EsEdicion => IdReceta is not null;

    /// <summary>
    /// Desde un turno o al editar, el paciente ya está decidido y no se
    /// cambia. En una receta nueva se elige con el buscador.
    /// </summary>
    public bool PuedeCambiarPaciente => !EsEdicion && IdTurno is null;

    /// <summary>
    /// El formulario cargado con una receta que ya existe, para editarla. Las
    /// filas "Otro..." vuelven a separar el nombre escrito a mano de las
    /// indicaciones.
    /// </summary>
    public static RecetaCrearViewModel DesdeReceta(Receta receta, int idPaciente)
    {
        var filas = new List<RecetaMedicamentoCampoViewModel>();

        foreach (var medicamento in receta.Medicamentos)
        {
            var fila = new RecetaMedicamentoCampoViewModel
            {
                IdMedicamento = medicamento.IdMedicamento,
                Dosis = medicamento.Dosis,
                Frecuencia = medicamento.Frecuencia,
                Duracion = medicamento.Duracion,
                Indicaciones = medicamento.Indicaciones
            };

            if (MedicamentoService.EsMarcadorOtro(medicamento.Nombre, medicamento.NombreGenerico))
            {
                var (nombre, indicaciones) = IndicacionesDeOtro.Separar(medicamento.Indicaciones);
                fila.IdMedicamento = IdOtro;
                fila.NombreOtro = nombre;
                fila.Indicaciones = indicaciones;
            }

            filas.Add(fila);
        }

        return new RecetaCrearViewModel
        {
            IdReceta = receta.IdReceta,
            IdTurno = receta.IdTurno,
            IdPaciente = idPaciente,
            Fecha = receta.Fecha.Date,
            Vigencia = receta.Vigencia.Date,
            Detalles = receta.Detalles,
            Medicamentos = filas
        };
    }

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

            if (!fila.EsOtro) continue;

            if (string.IsNullOrWhiteSpace(fila.NombreOtro))
            {
                yield return new ValidationResult(
                    "Escribí el nombre del medicamento.",
                    new[] { $"{nameof(Medicamentos)}[{i}].{nameof(RecetaMedicamentoCampoViewModel.NombreOtro)}" });
            }
            else if (IndicacionesDeOtro.Componer(fila.NombreOtro, fila.Indicaciones).Length
                     > RecetaMedicamentoCampoViewModel.LargoMaximoDeIndicaciones)
            {
                // El nombre se guarda dentro de las indicaciones: comparten el límite.
                yield return new ValidationResult(
                    "El nombre del medicamento y las indicaciones juntos no pueden superar los 300 caracteres.",
                    new[] { $"{nameof(Medicamentos)}[{i}].{nameof(RecetaMedicamentoCampoViewModel.NombreOtro)}" });
            }
        }

        // Las filas "Otro..." comparten el valor del desplegable sin ser el mismo medicamento.
        if (cargados.Where(m => m.IdMedicamento is not null && !m.EsOtro)
                    .GroupBy(m => m.IdMedicamento)
                    .Any(g => g.Count() > 1))
        {
            // Sin member name: va al resumen de arriba del formulario, porque
            // no corresponde a una sola fila.
            yield return new ValidationResult("Hay un medicamento repetido en la receta.");
        }
    }
}
