using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models.ViewModels;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Pantalla Usuarios/Editar: el administrador edita el perfil de otra persona.
/// Cada tarjeta es un formulario aparte con su propio sub-modelo y su propio
/// POST, así un error en una no pierde lo cargado en la otra ni deja un
/// guardado a medias entre dos llamadas a la API.
/// </summary>
public class UsuarioEditarViewModel
{
    public int IdUsuario { get; init; }

    /// <summary>Null cuando se entró sin ficha de paciente: solo se edita la cuenta.</summary>
    public int? IdPaciente { get; init; }

    // Encabezado: sale siempre de la API, nunca del formulario enviado.
    public string NombreCompleto { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;

    public CuentaEditarViewModel Cuenta { get; set; } = new();

    public PacienteEditarViewModel? Paciente { get; set; }
}

/// <summary>
/// Datos de la cuenta. Espeja lo que se usa de UsuarioUpdateDto: la contraseña
/// queda afuera a propósito y el email la API no lo deja cambiar.
/// </summary>
public class CuentaEditarViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El apellido no puede superar los 60 caracteres.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [MaxLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}

/// <summary>
/// Ficha del paciente. Espeja PacienteUpdateDto de la API (sin la foto): todos
/// los campos son opcionales.
/// </summary>
public class PacienteEditarViewModel
{
    [MaxLength(15, ErrorMessage = "El DNI no puede superar los 15 caracteres.")]
    [Display(Name = "DNI")]
    public string? Dni { get; set; }

    [NoFutura(ErrorMessage = "La fecha de nacimiento no puede ser futura.")]
    [Display(Name = "Fecha de nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }

    [Display(Name = "Sexo")]
    public string? Sexo { get; set; }

    [Display(Name = "Grupo sanguíneo")]
    public string? GrupoSanguineo { get; set; }

    [MaxLength(60, ErrorMessage = "La nacionalidad no puede superar los 60 caracteres.")]
    [Display(Name = "Nacionalidad")]
    public string? Nacionalidad { get; set; }

    [Display(Name = "Estado civil")]
    public string? EstadoCivil { get; set; }

    [MaxLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [StringLength(500, ErrorMessage = "Las alergias no pueden superar los 500 caracteres.")]
    [Display(Name = "Alergias")]
    public string? Alergias { get; set; }

    [StringLength(500, ErrorMessage = "Las condiciones no pueden superar los 500 caracteres.")]
    [Display(Name = "Condiciones preexistentes")]
    public string? Condiciones { get; set; }

    // Opciones de los selects. Son las mismas listas fijas de MiPerfil y de
    // Pacientes/Crear: la API no expone catálogos.

    private static readonly string[] SexosFijos = { "femenino", "masculino", "otro" };

    private static readonly string[] GruposSanguineosFijos =
        { "A+", "A-", "B+", "B-", "AB+", "AB-", "0+", "0-" };

    private static readonly string[] EstadosCivilesFijos =
        { "Soltero/a", "Casado/a", "Divorciado/a", "Viudo/a", "Unión convivencial" };

    [ValidateNever] public IEnumerable<SelectListItem> Sexos => Opciones(SexosFijos, Sexo);
    [ValidateNever] public IEnumerable<SelectListItem> GruposSanguineos => Opciones(GruposSanguineosFijos, GrupoSanguineo);
    [ValidateNever] public IEnumerable<SelectListItem> EstadosCiviles => Opciones(EstadosCivilesFijos, EstadoCivil);

    /// <summary>
    /// Si el valor guardado no está en la lista fija (la API acepta cualquier
    /// texto) se lo suma como opción, para que el select lo muestre en vez de
    /// aparentar que el dato está vacío.
    /// </summary>
    private static IEnumerable<SelectListItem> Opciones(string[] fijas, string? actual)
    {
        var valores = string.IsNullOrWhiteSpace(actual) || fijas.Contains(actual)
            ? fijas
            : fijas.Append(actual);

        return valores.Select(valor => new SelectListItem(valor, valor)).ToList();
    }
}
