using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models.ViewModels;
using ChronoSaludWeb.Services;

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

    /// <summary>Según la API. Decide si el avatar pide la foto y si se ofrece quitarla.</summary>
    public bool TieneFoto { get; init; }

    public CuentaEditarViewModel Cuenta { get; set; } = new();

    public PacienteEditarViewModel? Paciente { get; set; }

    /// <summary>Null si el usuario no tiene perfil de doctor.</summary>
    public int? IdDoctor { get; init; }

    /// <summary>Solo se muestra: la API no deja cambiar la matrícula.</summary>
    public string? Matricula { get; init; }

    public DoctorEditarViewModel? Doctor { get; set; }

    /// <summary>
    /// La API informó un perfil de doctor para este usuario. Puede ser true
    /// aunque Doctor quede en null (si el perfil existe pero no se pudo traer).
    /// </summary>
    public bool TienePerfilDoctor { get; init; }

    /// <summary>
    /// Usuario con rol doctor al que le falta la fila de Doctores (por ejemplo,
    /// un alta que quedó a mitad de camino). Lo calcula el servidor y solo es
    /// true si se pudo confirmar contra la API que no tiene perfil: ante la
    /// duda queda en false, para no ofrecer crear un segundo perfil.
    /// </summary>
    public bool FaltaPerfilDoctor { get; init; }

    public DoctorNuevoViewModel DoctorNuevo { get; set; } = new();
}

/// <summary>
/// Paso 2 del alta de doctor para un usuario que ya existe: completa su fila
/// de Doctores (POST /doctores). Pide lo mismo que el alta de doctor
/// (NuevoDoctorViewModel) para esos tres campos; el usuario ya está elegido.
/// </summary>
public class DoctorNuevoViewModel
{
    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    [Display(Name = "Especialidad")]
    public string Especialidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "La matrícula es obligatoria.")]
    [Display(Name = "Matrícula")]
    public string Matricula { get; set; } = string.Empty;

    [Display(Name = "Consultorio")]
    public string? Consultorio { get; set; }
}

/// <summary>
/// Perfil de doctor. Espeja DoctorUpdateDto de la API.
/// </summary>
public class DoctorEditarViewModel
{
    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    [MaxLength(80, ErrorMessage = "La especialidad no puede superar los 80 caracteres.")]
    [Display(Name = "Especialidad")]
    public string Especialidad { get; set; } = string.Empty;

    [MaxLength(60, ErrorMessage = "El consultorio no puede superar los 60 caracteres.")]
    [Display(Name = "Consultorio")]
    public string? Consultorio { get; set; }
}

public class UsuarioFilaViewModel
{
    public int IdUsuario { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public bool TieneFoto { get; init; }

    /// <summary>Nombre crudo para el avatar: vacío si la API no mandó ninguno.</summary>
    public string NombreCrudo => $"{Nombre} {Apellido}".Trim();

    public string NombreCompleto =>
        string.IsNullOrWhiteSpace(NombreCrudo) ? "Sin datos" : NombreCrudo;
}

public class UsuariosIndexViewModel
{
    public const int PorPagina = 20;

    /// <summary>Roles por los que se puede filtrar, en minúscula como en la API.</summary>
    public static readonly string[] RolesFiltrables = { "paciente", "doctor", "administrador" };

    public IReadOnlyList<UsuarioFilaViewModel> Usuarios { get; init; } = Array.Empty<UsuarioFilaViewModel>();
    public int Total { get; init; }
    public int Pagina { get; init; } = 1;

    /// <summary>Texto del buscador. La API lo resuelve como "contiene".</summary>
    public string? Buscar { get; init; }

    /// <summary>Null es "todos los roles".</summary>
    public string? Rol { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public bool HayFiltro => !string.IsNullOrWhiteSpace(Buscar) || Rol is not null;
    public bool SinResultados => Usuarios.Count == 0 && HayFiltro;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)PorPagina));
    public bool HayAnterior => Pagina > 1;
    public bool HaySiguiente => Pagina < TotalPaginas;

    public IEnumerable<SelectListItem> Roles => RolesFiltrables
        .Select(rol => new SelectListItem(char.ToUpperInvariant(rol[0]) + rol[1..], rol, rol == Rol))
        .ToList();
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
/// los campos son opcionales. Un campo que se deja vacío borra el dato guardado
/// (ver <see cref="ArmarFicha"/>).
/// </summary>
public class PacienteEditarViewModel
{
    [Display(Name = "Tipo de documento")]
    public string? TipoDocumento { get; set; }

    [MaxLength(15, ErrorMessage = "El DNI no puede superar los 15 caracteres.")]
    [Display(Name = "Número de documento")]
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

    [Display(Name = "Provincia")]
    public string? Provincia { get; set; }

    [MaxLength(100, ErrorMessage = "La localidad no puede superar los 100 caracteres.")]
    [Display(Name = "Localidad")]
    public string? Localidad { get; set; }

    [MaxLength(10, ErrorMessage = "El código postal no puede superar los 10 caracteres.")]
    [Display(Name = "Código postal")]
    public string? CodigoPostal { get; set; }

    [MaxLength(120, ErrorMessage = "El nombre del contacto no puede superar los 120 caracteres.")]
    [Display(Name = "Contacto de emergencia")]
    public string? ContactoEmergenciaNombre { get; set; }

    [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
    [MaxLength(30, ErrorMessage = "El teléfono del contacto no puede superar los 30 caracteres.")]
    [Display(Name = "Teléfono del contacto")]
    public string? ContactoEmergenciaTelefono { get; set; }

    [StringLength(500, ErrorMessage = "Las alergias no pueden superar los 500 caracteres.")]
    [Display(Name = "Alergias")]
    public string? Alergias { get; set; }

    [StringLength(500, ErrorMessage = "Las condiciones no pueden superar los 500 caracteres.")]
    [Display(Name = "Condiciones preexistentes")]
    public string? Condiciones { get; set; }

    // Opciones de los selects (listas fijas de OpcionesPaciente). Si el valor
    // guardado no está en la lista, se suma como opción para que se vea.

    [ValidateNever] public List<SelectListItem> TiposDocumento => OpcionesPaciente.Opciones(OpcionesPaciente.TiposDocumento, TipoDocumento);
    [ValidateNever] public List<SelectListItem> Sexos => OpcionesPaciente.Opciones(OpcionesPaciente.Sexos, Sexo);
    [ValidateNever] public List<SelectListItem> GruposSanguineos => OpcionesPaciente.Opciones(OpcionesPaciente.GruposSanguineos, GrupoSanguineo);
    [ValidateNever] public List<SelectListItem> EstadosCiviles => OpcionesPaciente.Opciones(OpcionesPaciente.EstadosCiviles, EstadoCivil);
    [ValidateNever] public List<SelectListItem> Provincias => OpcionesPaciente.Opciones(OpcionesPaciente.Provincias, Provincia);

    /// <summary>
    /// La ficha como la espera PUT /pacientes/{id}. Cada campo que quedó vacío
    /// va en la lista Borrar: así, vaciar un campo en el formulario borra el
    /// dato guardado (si ya estaba vacío, no pasa nada).
    /// </summary>
    public DatosFichaPaciente ArmarFicha()
    {
        var ficha = new DatosFichaPaciente();

        ficha.TipoDocumento = Limpio(TipoDocumento);
        ficha.Dni = Limpio(Dni);
        ficha.Sexo = Limpio(Sexo);
        ficha.GrupoSanguineo = Limpio(GrupoSanguineo);
        ficha.Nacionalidad = Limpio(Nacionalidad);
        ficha.EstadoCivil = Limpio(EstadoCivil);
        ficha.Direccion = Limpio(Direccion);
        ficha.Provincia = Limpio(Provincia);
        ficha.Localidad = Limpio(Localidad);
        ficha.CodigoPostal = Limpio(CodigoPostal);
        ficha.ContactoEmergenciaNombre = Limpio(ContactoEmergenciaNombre);
        ficha.ContactoEmergenciaTelefono = Limpio(ContactoEmergenciaTelefono);
        ficha.Alergias = Limpio(Alergias);
        ficha.Condiciones = Limpio(Condiciones);

        if (FechaNacimiento != null)
            ficha.FechaNacimiento = FechaNacimiento.Value.ToDateTime(TimeOnly.MinValue);

        // Los nombres son los que entiende la API (FichaPaciente.BorrarCampos).
        var borrar = new List<string>();
        if (ficha.TipoDocumento == null) borrar.Add("tipoDocumento");
        if (ficha.Dni == null) borrar.Add("dni");
        if (ficha.FechaNacimiento == null) borrar.Add("fechaNacimiento");
        if (ficha.Sexo == null) borrar.Add("sexo");
        if (ficha.GrupoSanguineo == null) borrar.Add("grupoSanguineo");
        if (ficha.Nacionalidad == null) borrar.Add("nacionalidad");
        if (ficha.EstadoCivil == null) borrar.Add("estadoCivil");
        if (ficha.Direccion == null) borrar.Add("direccion");
        if (ficha.Provincia == null) borrar.Add("provincia");
        if (ficha.Localidad == null) borrar.Add("localidad");
        if (ficha.CodigoPostal == null) borrar.Add("codigoPostal");
        if (ficha.ContactoEmergenciaNombre == null) borrar.Add("contactoEmergenciaNombre");
        if (ficha.ContactoEmergenciaTelefono == null) borrar.Add("contactoEmergenciaTelefono");
        if (ficha.Alergias == null) borrar.Add("alergias");
        if (ficha.Condiciones == null) borrar.Add("condiciones");

        ficha.Borrar = borrar;
        return ficha;
    }

    /// <summary>El texto sin espacios al principio ni al final; null si quedó vacío.</summary>
    private static string? Limpio(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        return texto.Trim();
    }
}
