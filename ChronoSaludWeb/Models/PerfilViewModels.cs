using System.ComponentModel.DataAnnotations;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Pantalla Mi perfil: los datos de quien está logueado, en tarjetas de solo
/// lectura. Lo que se puede cambiar se cambia en Mi perfil/Editar.
/// </summary>
public class MiPerfilViewModel
{
    // Tarjeta "Tu cuenta".
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public string Rol { get; init; } = string.Empty;

    /// <summary>
    /// Tarjeta "Tu ficha", solo para el paciente. Null para los otros roles, o
    /// si al paciente todavía no le crearon el perfil.
    /// </summary>
    public PacienteDetalleViewModel? Ficha { get; init; }

    /// <summary>Paciente sin perfil de paciente: se le avisa en vez de mostrar la ficha.</summary>
    public bool FaltaFicha { get; init; }

    // Tarjeta "Datos profesionales", solo para el doctor. IdDoctor queda en
    // null para los otros roles o si no se pudo traer el perfil.
    public int? IdDoctor { get; init; }
    public string? Especialidad { get; init; }
    public string? Matricula { get; init; }
    public string? Consultorio { get; init; }

    public string NombreCompleto =>
        string.IsNullOrWhiteSpace($"{Nombre}{Apellido}") ? "Sin datos" : $"{Nombre} {Apellido}".Trim();

    public bool EsPaciente => Rol == "paciente";
    public bool EsDoctor => Rol == "doctor";
}

/// <summary>
/// Pantalla Mi perfil/Editar. Igual que Usuarios/Editar, cada tarjeta es un
/// formulario aparte con su propio sub-modelo y su propio POST: un error en
/// una no pierde lo cargado en la otra.
/// </summary>
public class MiPerfilEditarViewModel
{
    // Encabezado: sale siempre de la API, nunca del formulario enviado.
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;

    public CuentaEditarViewModel Cuenta { get; set; } = new();

    /// <summary>Id de la ficha, de /pacientes/me. Null si no es paciente o no tiene ficha.</summary>
    public int? IdPaciente { get; init; }

    /// <summary>La ficha, solo para el paciente que ya tiene perfil de paciente.</summary>
    public PacienteEditarViewModel? Paciente { get; set; }

    /// <summary>
    /// El DNI que ya está guardado. El paciente lo ve pero no lo puede cambiar
    /// (la API no lo deja): si hay que corregirlo, lo hace la administración.
    /// Null si todavía no cargó ninguno, y entonces se muestra el campo.
    /// </summary>
    public string? DniGuardado { get; init; }
}

/// <summary>
/// Cambio de la contraseña propia. La API vuelve a revisar todo: que la actual
/// coincida, el largo y que la nueva sea distinta.
/// </summary>
public class CambioContrasenaViewModel
{
    [Required(ErrorMessage = "Escribí tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string Actual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribí la contraseña nueva.")]
    [MinLength(8, ErrorMessage = "La contraseña nueva debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña nueva")]
    public string Nueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña nueva.")]
    [Compare(nameof(Nueva), ErrorMessage = "Las dos contraseñas nuevas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetí la contraseña nueva")]
    public string Repetir { get; set; } = string.Empty;
}
