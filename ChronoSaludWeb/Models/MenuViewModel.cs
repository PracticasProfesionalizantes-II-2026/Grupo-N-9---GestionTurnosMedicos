using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>Un destino del menú principal.</summary>
public class ItemMenuViewModel
{
    public required string Titulo { get; init; }

    /// <summary>
    /// Etiqueta para la barra inferior del celular, donde el título largo no
    /// entra. Null cuando el título ya es corto.
    /// </summary>
    public string? TituloCorto { get; init; }

    /// <summary>Clave del ícono; el partial la traduce a un SVG.</summary>
    public required string Icono { get; init; }

    public required string Controlador { get; init; }
    public required string Accion { get; init; }

    /// <summary>Es la sección que se está viendo.</summary>
    public bool Activo { get; init; }

    /// <summary>Va en la barra inferior; los demás quedan detrás de "Más".</summary>
    public bool EnBarra { get; init; }

    public string Etiqueta => TituloCorto ?? Titulo;
}

/// <summary>
/// El menú de quien está mirando la página. El layout lo arma una sola vez por
/// render y lo leen el encabezado, el menú de cuenta y la barra inferior, para
/// que las tres partes muestren lo mismo sin repetir las condiciones de rol.
/// </summary>
public class MenuViewModel
{
    public bool HaySesion { get; init; }
    public string? Nombre { get; init; }
    public string? Rol { get; init; }

    /// <summary>Todos los destinos que ve este usuario, en el orden del encabezado.</summary>
    public IReadOnlyList<ItemMenuViewModel> Items { get; init; } = Array.Empty<ItemMenuViewModel>();

    /// <summary>Ajustes no es un destino del menú principal, pero también se marca.</summary>
    public bool AjustesActivo { get; init; }

    /// <summary>Mi perfil tampoco: va en el menú de la cuenta y en "Más".</summary>
    public bool MiPerfilActivo { get; init; }

    public IEnumerable<ItemMenuViewModel> Barra => Items.Where(item => item.EnBarra);

    public IEnumerable<ItemMenuViewModel> Resto => Items.Where(item => !item.EnBarra);

    /// <summary>
    /// La pantalla actual está detrás de "Más": se marca ese botón para que en
    /// el celular igual se vea dónde está uno parado.
    /// </summary>
    public bool MasActivo => AjustesActivo || MiPerfilActivo || Resto.Any(item => item.Activo);

    /// <summary>
    /// Qué ve cada uno lo deciden las mismas banderas de <see cref="AuthService"/>
    /// que usaba el layout. Es un filtro de interfaz: cada controlador vuelve a
    /// chequear el permiso.
    /// </summary>
    public static MenuViewModel Armar(AuthService auth, string? controlador, string? accion)
    {
        var sesion = auth.SesionActual;
        var ajustesActivo = Es(controlador, "Ajustes");

        if (sesion is null)
            return new MenuViewModel { AjustesActivo = ajustesActivo };

        var puedeVerPacientes = auth.PuedeVerPacientes;
        var esAdministrador = auth.EsAdministrador;

        // En la barra inferior entran cuatro destinos más "Más": van los que
        // cada rol usa más seguido.
        string[] enBarra = esAdministrador ? ["Home", "Turnos", "Pacientes", "Admin"]
            : puedeVerPacientes ? ["Home", "Turnos", "Pacientes", "Recetas"]
            : ["Home", "Turnos", "Recetas", "Historial"];

        var items = new List<ItemMenuViewModel>();

        // Sin soloEnAccion alcanza con el controlador, así "Turnos" sigue
        // activo en Crear o en Detalle. Inicio la pide porque el controlador
        // Home también sirve Privacidad.
        void Agregar(string titulo, string icono, string destino, string? corto = null, string? soloEnAccion = null) =>
            items.Add(new ItemMenuViewModel
            {
                Titulo = titulo,
                TituloCorto = corto,
                Icono = icono,
                Controlador = destino,
                Accion = "Index",
                Activo = Es(controlador, destino) && (soloEnAccion is null || Es(accion, soloEnAccion)),
                EnBarra = enBarra.Contains(destino)
            });

        Agregar("Inicio", "casa", "Home", soloEnAccion: "Index");
        Agregar("Turnos", "calendario", "Turnos");
        if (puedeVerPacientes) Agregar("Pacientes", "personas", "Pacientes");
        Agregar("Doctores", "estetoscopio", "Doctores");
        Agregar("Recetas", "pastilla", "Recetas");
        Agregar("Historia clínica", "historia", "Historial", corto: "Historia");
        if (esAdministrador) Agregar("Cuentas", "persona-mas", "Admin");

        return new MenuViewModel
        {
            HaySesion = true,
            Nombre = sesion.Nombre,
            Rol = sesion.Rol,
            Items = items,
            AjustesActivo = ajustesActivo,
            MiPerfilActivo = Es(controlador, "MiPerfil")
        };
    }

    private static bool Es(string? valor, string esperado) =>
        string.Equals(valor, esperado, StringComparison.OrdinalIgnoreCase);
}
