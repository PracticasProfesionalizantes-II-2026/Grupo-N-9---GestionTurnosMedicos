using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Models;

/// <summary>Una fila del historial de movimientos, lista para mostrar.</summary>
public class MovimientoFilaViewModel
{
    /// <summary>Cuándo fue, ya en hora de Argentina (la API lo manda en UTC).</summary>
    public DateTime Fecha { get; init; }

    /// <summary>La acción con su nombre legible: "Creó un turno".</summary>
    public string Accion { get; init; } = string.Empty;

    public string Resumen { get; init; } = string.Empty;

    /// <summary>Nombre del doctor involucrado. Null en "Mi actividad", donde siempre es el propio.</summary>
    public string? Doctor { get; init; }

    /// <summary>Quién lo hizo. Null para el doctor: la API no le manda el nombre.</summary>
    public string? Usuario { get; init; }

    /// <summary>El rol que tenía quien lo hizo en ese momento.</summary>
    public string Rol { get; init; } = string.Empty;

    public string FechaMostrada => Fecha.ToString("d MMM yyyy, HH:mm", TurnosIndexViewModel.Cultura);

    /// <summary>Para el atributo datetime de la etiqueta time.</summary>
    public string FechaIso => Fecha.ToString("yyyy-MM-dd'T'HH:mm");

    public static MovimientoFilaViewModel Desde(MovimientoLista movimiento, string? doctor) => new()
    {
        Fecha = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(movimiento.FechaUtc.ToUniversalTime(), DateTimeKind.Utc), FechaArgentina.Zona),
        Accion = ActividadViewModel.NombreDeAccion(movimiento.Accion),
        Resumen = movimiento.Resumen,
        Doctor = doctor,
        Usuario = string.IsNullOrWhiteSpace(movimiento.Usuario) ? null : movimiento.Usuario.Trim(),
        Rol = movimiento.RolUsuario
    };
}

/// <summary>
/// Las pantallas "Actividad" (administrador: todo el historial) y "Mi
/// actividad" (doctor: lo de su agenda y su horario). Es la misma vista; con
/// <see cref="EsPropia"/> no hay filtro ni columna de doctor, ni nombre de
/// quien hizo cada cambio.
/// </summary>
public class ActividadViewModel
{
    public const int PorPagina = 20;

    /// <summary>
    /// Las acciones que registra la API (AccionesMovimiento) con el nombre que
    /// se muestra, en el orden del select.
    /// </summary>
    public static readonly IReadOnlyList<(string Codigo, string Nombre)> Acciones = new[]
    {
        ("turno.creado",          "Creó un turno"),
        ("turno.confirmado",      "Confirmó un turno"),
        ("turno.reprogramado",    "Reprogramó un turno"),
        ("turno.completado",      "Completó un turno"),
        ("turno.ausente",         "Marcó ausente a un paciente"),
        ("turno.cancelado",       "Canceló un turno"),
        ("turno.estado_cambiado", "Cambió el estado de un turno"),
        ("horario.cambiado",      "Cambió un horario"),
    };

    /// <summary>El nombre legible de una acción; si la API manda una que no está en la lista, su código.</summary>
    public static string NombreDeAccion(string codigo)
    {
        foreach (var (conocido, nombre) in Acciones)
        {
            if (conocido == codigo) return nombre;
        }

        return codigo;
    }

    public static bool EsAccionConocida(string? codigo) => Acciones.Any(a => a.Codigo == codigo);

    /// <summary>True en "Mi actividad".</summary>
    public bool EsPropia { get; init; }

    public IReadOnlyList<MovimientoFilaViewModel> Movimientos { get; init; } = Array.Empty<MovimientoFilaViewModel>();
    public int Total { get; init; }
    public int Pagina { get; init; } = 1;

    /// <summary>IdDoctor por el que se filtra. Solo en "Actividad"; null es "todos".</summary>
    public int? Doctor { get; init; }

    /// <summary>Código de la acción por la que se filtra; null es "todas".</summary>
    public string? Accion { get; init; }

    /// <summary>Días de Argentina, los dos incluidos.</summary>
    public DateTime? Desde { get; init; }
    public DateTime? Hasta { get; init; }

    /// <summary>Opciones del select de doctor. Vacío en "Mi actividad".</summary>
    public IReadOnlyList<SelectListItem> Doctores { get; init; } = Array.Empty<SelectListItem>();

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    /// <summary>No se puede mostrar nada y no es una falla: el doctor todavía no tiene perfil.</summary>
    public string? Aviso { get; init; }

    public string Titulo => EsPropia ? "Mi actividad" : "Actividad";

    /// <summary>La acción del controlador que arma esta pantalla: a ella apuntan el formulario y el paginado.</summary>
    public string AccionDeRuta => EsPropia ? "Mia" : "Index";

    public IEnumerable<SelectListItem> OpcionesDeAccion =>
        Acciones.Select(a => new SelectListItem(a.Nombre, a.Codigo, a.Codigo == Accion)).ToList();

    public bool HayFiltro => Doctor is not null || Accion is not null || Desde is not null || Hasta is not null;
    public bool SinResultados => Movimientos.Count == 0 && HayFiltro;
    public bool RangoInvertido => Desde is { } desde && Hasta is { } hasta && desde.Date > hasta.Date;

    /// <summary>Lo que muestra el paginador compartido (Shared/_Paginador).</summary>
    public PaginadorViewModel Paginador => new()
    {
        Pagina = Pagina,
        TotalPaginas = PaginadorViewModel.ContarPaginas(Total, PorPagina),
        Accion = AccionDeRuta,
        RutaAnterior = Ruta(Pagina - 1),
        RutaSiguiente = Ruta(Pagina + 1)
    };

    /// <summary>
    /// Los filtros actuales como valores de ruta, para los enlaces que tienen
    /// que conservarlos. Un filtro vacío no viaja, y la primera página tampoco.
    /// </summary>
    public IDictionary<string, string> Ruta(int pagina = 1)
    {
        var ruta = new Dictionary<string, string>();

        if (Doctor is { } doctor) ruta["doctor"] = $"{doctor}";
        if (Accion is not null) ruta["accion"] = Accion;
        if (Desde is { } desde) ruta["desde"] = desde.ToString("yyyy-MM-dd");
        if (Hasta is { } hasta) ruta["hasta"] = hasta.ToString("yyyy-MM-dd");
        if (pagina > 1) ruta["pagina"] = $"{pagina}";

        return ruta;
    }
}
