using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChronoSaludWeb.Models.ViewModels;

/// <summary>
/// Alta de paciente hecha por un administrador (Pacientes/Crear). Por ahora solo
/// Nombre, Apellido, Email, Contraseña y Teléfono llegan a la API
/// (AuthService.RegistrarComoPacienteAsync): el resto se valida y se acepta,
/// pero el registro todavía no lo recibe.
/// </summary>
public class PacienteCreateViewModel
{
    // Datos personales

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El apellido no puede superar los 60 caracteres.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
    [Display(Name = "Tipo de documento")]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [MaxLength(15, ErrorMessage = "El número de documento no puede superar los 15 caracteres.")]
    [Display(Name = "Número de documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    /// <summary>Nullable para que [Required] pueda fallar cuando llega vacío.</summary>
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    [NoFutura(ErrorMessage = "La fecha de nacimiento no puede ser futura.")]
    [Display(Name = "Fecha de nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }

    [Required(ErrorMessage = "El género es obligatorio.")]
    [Display(Name = "Género")]
    public string Genero { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nacionalidad es obligatoria.")]
    [Display(Name = "Nacionalidad")]
    public string Nacionalidad { get; set; } = "Argentina";

    [Required(ErrorMessage = "El estado civil es obligatorio.")]
    [Display(Name = "Estado civil")]
    public string EstadoCivil { get; set; } = string.Empty;

    [Display(Name = "Foto")]
    public IFormFile? Foto { get; set; }

    [Display(Name = "Grupo sanguíneo")]
    public string? GrupoSanguineo { get; set; }

    [Display(Name = "Tiene obra social")]
    public bool TieneObraSocial { get; set; }

    [Display(Name = "Obra social")]
    public string? ObraSocial { get; set; }

    [Display(Name = "Número de afiliado")]
    public string? NumeroAfiliado { get; set; }

    // Datos de contacto y cuenta

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Obligatorio: es el usuario con el que el paciente va a ingresar.</summary>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// La clave la tipea el admin para otra persona: un error de tipeo la
    /// dejaría sin acceso, por eso se pide dos veces.
    /// </summary>
    [Required(ErrorMessage = "Repetí la contraseña.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [Display(Name = "Dirección")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La provincia es obligatoria.")]
    [Display(Name = "Provincia")]
    public string Provincia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La localidad es obligatoria.")]
    [Display(Name = "Localidad")]
    public string Localidad { get; set; } = string.Empty;

    [Display(Name = "Código postal")]
    public string? CodigoPostal { get; set; }

    // Contacto de emergencia

    [Display(Name = "Nombre del contacto")]
    public string? ContactoEmergenciaNombre { get; set; }

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [Display(Name = "Teléfono del contacto")]
    public string? ContactoEmergenciaTelefono { get; set; }

    [DataType(DataType.MultilineText)]
    [Display(Name = "Alergias")]
    public string? Alergias { get; set; }

    // Opciones de los selects. Son listas fijas: la API no expone catálogos.
    // asp-for marca la opción elegida, así que se pueden compartir entre pedidos.

    private static readonly SelectList TiposDocumentoFijos =
        new(new[] { "DNI", "LC", "LE", "Pasaporte" });

    private static readonly SelectList GenerosFijos =
        new(new[] { "Femenino", "Masculino", "No binario", "Prefiero no decirlo" });

    private static readonly SelectList EstadosCivilesFijos =
        new(new[] { "Soltero/a", "Casado/a", "Divorciado/a", "Viudo/a", "Unión convivencial" });

    private static readonly SelectList GruposSanguineosFijos =
        new(new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "0+", "0-" });

    private static readonly SelectList ProvinciasFijas = new(new[]
    {
        "Buenos Aires",
        "Catamarca",
        "Chaco",
        "Chubut",
        "Ciudad Autónoma de Buenos Aires",
        "Córdoba",
        "Corrientes",
        "Entre Ríos",
        "Formosa",
        "Jujuy",
        "La Pampa",
        "La Rioja",
        "Mendoza",
        "Misiones",
        "Neuquén",
        "Río Negro",
        "Salta",
        "San Juan",
        "San Luis",
        "Santa Cruz",
        "Santa Fe",
        "Santiago del Estero",
        "Tierra del Fuego, Antártida e Islas del Atlántico Sur",
        "Tucumán"
    });

    [BindNever, ValidateNever] public SelectList TiposDocumento => TiposDocumentoFijos;
    [BindNever, ValidateNever] public SelectList Generos => GenerosFijos;
    [BindNever, ValidateNever] public SelectList EstadosCiviles => EstadosCivilesFijos;
    [BindNever, ValidateNever] public SelectList GruposSanguineos => GruposSanguineosFijos;
    [BindNever, ValidateNever] public SelectList Provincias => ProvinciasFijas;
}

/// <summary>Rechaza fechas posteriores a hoy. Un valor vacío lo resuelve [Required].</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NoFuturaAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is not DateOnly fecha || fecha <= DateOnly.FromDateTime(DateTime.Today);
}
