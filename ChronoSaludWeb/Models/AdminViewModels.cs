using System.ComponentModel.DataAnnotations;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Alta de doctor en un solo formulario, que internamente hace dos llamadas:
/// crear el Usuario (rol "doctor") y después POST /doctores con ese IdUsuario.
/// No hay transacción entre las dos, así que si la primera funciona y la
/// segunda falla, el Usuario ya quedó creado (ver AdminController.NuevoDoctor).
/// </summary>
public class NuevoDoctorViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    [Display(Name = "Especialidad")]
    public string Especialidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "La matrícula es obligatoria.")]
    [Display(Name = "Matrícula")]
    public string Matricula { get; set; } = string.Empty;

    [Display(Name = "Consultorio")]
    public string? Consultorio { get; set; }

    /// <summary>
    /// Se completa solo cuando el paso 1 (crear Usuario) tuvo éxito pero el
    /// paso 2 (POST /doctores) falló: le da al admin el link para retomar
    /// desde CompletarDoctor sin recrear el usuario.
    /// </summary>
    public int? IdUsuarioCreado { get; set; }
}

/// <summary>
/// Paso 2 del alta de doctor, aislado: completa la fila de Doctores de un
/// Usuario que ya existe. Sirve tanto para retomar un alta que falló a mitad
/// de camino como para cualquier Usuario rol doctor sin perfil todavía.
/// </summary>
public class CompletarDoctorViewModel
{
    [Required(ErrorMessage = "El IdUsuario es obligatorio.")]
    [Display(Name = "Id de usuario")]
    public int? IdUsuario { get; set; }

    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    [Display(Name = "Especialidad")]
    public string Especialidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "La matrícula es obligatoria.")]
    [Display(Name = "Matrícula")]
    public string Matricula { get; set; } = string.Empty;

    [Display(Name = "Consultorio")]
    public string? Consultorio { get; set; }
}

public class NuevoAdministradorViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}
