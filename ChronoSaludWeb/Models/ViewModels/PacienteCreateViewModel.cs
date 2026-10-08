using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models.ViewModels;

/// <summary>
/// Alta de paciente hecha por un administrador (Pacientes/Crear). La cuenta
/// (nombre, apellido, email, contraseña y teléfono) y la ficha (el resto) se
/// mandan juntas a la API, que las guarda en una sola operación.
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

    [Display(Name = "Sexo")]
    public string? Sexo { get; set; }

    [Required(ErrorMessage = "La nacionalidad es obligatoria.")]
    [MaxLength(60, ErrorMessage = "La nacionalidad no puede superar los 60 caracteres.")]
    [Display(Name = "Nacionalidad")]
    public string Nacionalidad { get; set; } = "Argentina";

    [Required(ErrorMessage = "El estado civil es obligatorio.")]
    [Display(Name = "Estado civil")]
    public string EstadoCivil { get; set; } = string.Empty;

    [Display(Name = "Foto")]
    public IFormFile? Foto { get; set; }

    [Display(Name = "Grupo sanguíneo")]
    public string? GrupoSanguineo { get; set; }

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
    [MaxLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
    [Display(Name = "Dirección")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La provincia es obligatoria.")]
    [Display(Name = "Provincia")]
    public string Provincia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La localidad es obligatoria.")]
    [MaxLength(100, ErrorMessage = "La localidad no puede superar los 100 caracteres.")]
    [Display(Name = "Localidad")]
    public string Localidad { get; set; } = string.Empty;

    [MaxLength(10, ErrorMessage = "El código postal no puede superar los 10 caracteres.")]
    [Display(Name = "Código postal")]
    public string? CodigoPostal { get; set; }

    // Contacto de emergencia

    [MaxLength(120, ErrorMessage = "El nombre del contacto no puede superar los 120 caracteres.")]
    [Display(Name = "Nombre del contacto")]
    public string? ContactoEmergenciaNombre { get; set; }

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [MaxLength(30, ErrorMessage = "El teléfono del contacto no puede superar los 30 caracteres.")]
    [Display(Name = "Teléfono del contacto")]
    public string? ContactoEmergenciaTelefono { get; set; }

    [StringLength(500, ErrorMessage = "Las alergias no pueden superar los 500 caracteres.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Alergias")]
    public string? Alergias { get; set; }

    // Opciones de los selects (listas fijas de OpcionesPaciente). asp-for
    // marca sola la opción elegida.

    [BindNever, ValidateNever] public List<SelectListItem> TiposDocumento => OpcionesPaciente.Opciones(OpcionesPaciente.TiposDocumento, TipoDocumento);
    [BindNever, ValidateNever] public List<SelectListItem> Sexos => OpcionesPaciente.Opciones(OpcionesPaciente.Sexos, Sexo);
    [BindNever, ValidateNever] public List<SelectListItem> EstadosCiviles => OpcionesPaciente.Opciones(OpcionesPaciente.EstadosCiviles, EstadoCivil);
    [BindNever, ValidateNever] public List<SelectListItem> GruposSanguineos => OpcionesPaciente.Opciones(OpcionesPaciente.GruposSanguineos, GrupoSanguineo);
    [BindNever, ValidateNever] public List<SelectListItem> Provincias => OpcionesPaciente.Opciones(OpcionesPaciente.Provincias, Provincia);

    /// <summary>
    /// La ficha del paciente, como la espera la API en el alta. Los textos van
    /// sin espacios de más y los vacíos van como null (la API los ignora).
    /// </summary>
    public DatosFichaPaciente ArmarFicha()
    {
        var ficha = new DatosFichaPaciente();

        ficha.TipoDocumento = Limpio(TipoDocumento);
        ficha.Dni = Limpio(NumeroDocumento);
        ficha.Sexo = Limpio(Sexo);
        ficha.Nacionalidad = Limpio(Nacionalidad);
        ficha.EstadoCivil = Limpio(EstadoCivil);
        ficha.GrupoSanguineo = Limpio(GrupoSanguineo);
        ficha.Direccion = Limpio(Direccion);
        ficha.Provincia = Limpio(Provincia);
        ficha.Localidad = Limpio(Localidad);
        ficha.CodigoPostal = Limpio(CodigoPostal);
        ficha.ContactoEmergenciaNombre = Limpio(ContactoEmergenciaNombre);
        ficha.ContactoEmergenciaTelefono = Limpio(ContactoEmergenciaTelefono);
        ficha.Alergias = Limpio(Alergias);

        if (FechaNacimiento != null)
            ficha.FechaNacimiento = FechaNacimiento.Value.ToDateTime(TimeOnly.MinValue);

        return ficha;
    }

    /// <summary>El texto sin espacios al principio ni al final; null si quedó vacío.</summary>
    private static string? Limpio(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        return texto.Trim();
    }
}

/// <summary>Rechaza fechas posteriores a hoy. Un valor vacío lo resuelve [Required].</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NoFuturaAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is not DateOnly fecha || fecha <= DateOnly.FromDateTime(FechaArgentina.Hoy());
}
