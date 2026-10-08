namespace ChronoSaludWeb.Models;

/// <summary>
/// Lo que necesita el paginador compartido (Views/Shared/_Paginador): en qué
/// página se está, cuántas hay y a dónde llevan "Anterior" y "Siguiente".
/// Cada listado lo arma con sus propios filtros, así los enlaces los conservan.
/// </summary>
public class PaginadorViewModel
{
    public int Pagina { get; init; } = 1;
    public int TotalPaginas { get; init; } = 1;

    /// <summary>La acción del controlador actual a la que apuntan los enlaces.</summary>
    public string Accion { get; init; } = "Index";

    /// <summary>Los parámetros de la URL de la página anterior, con los filtros.</summary>
    public IDictionary<string, string> RutaAnterior { get; init; } = new Dictionary<string, string>();

    /// <summary>Los parámetros de la URL de la página siguiente, con los filtros.</summary>
    public IDictionary<string, string> RutaSiguiente { get; init; } = new Dictionary<string, string>();

    public bool HayAnterior => Pagina > 1;
    public bool HaySiguiente => Pagina < TotalPaginas;

    /// <summary>
    /// Cuántas páginas hacen falta para mostrar <paramref name="total"/>
    /// elementos de a <paramref name="porPagina"/>. Siempre al menos una.
    /// </summary>
    public static int ContarPaginas(int total, int porPagina)
    {
        var paginas = (total + porPagina - 1) / porPagina;
        return Math.Max(1, paginas);
    }
}
