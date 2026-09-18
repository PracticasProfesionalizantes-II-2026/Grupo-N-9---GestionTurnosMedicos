using System.ComponentModel.DataAnnotations;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Registro público de paciente. A propósito no tiene un campo Rol: la API
/// acepta cualquier valor en ese campo (ver AuthService.RegistrarPacienteAsync),
/// así que fijarlo en el código en vez de exponerlo acá es lo único que impide
/// que alguien se autoregistre como doctor o administrador desde esta pantalla.
/// </summary>
public class RegistroViewModel
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

    [Required(ErrorMessage = "Confirmá la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarContrasena { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}
