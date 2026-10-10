using System.ComponentModel.DataAnnotations;

namespace ChronoSaludWeb.Models;

/// <summary>"¿Olvidaste tu contraseña?": el email al que se manda el enlace.</summary>
public class RecuperarViewModel
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Ya se pidió el enlace: la vista muestra el aviso en lugar del formulario.</summary>
    public bool Enviado { get; set; }
}

/// <summary>La contraseña nueva, con el token que llegó en el enlace del email.</summary>
public class RestablecerViewModel
{
    /// <summary>Viaja oculto en el formulario. Si falta, la vista ofrece pedir otro enlace.</summary>
    [Required(ErrorMessage = "El enlace no es válido. Pedí uno nuevo.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña nueva")]
    public string Contrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Repetí la contraseña")]
    public string Repetir { get; set; } = string.Empty;
}
