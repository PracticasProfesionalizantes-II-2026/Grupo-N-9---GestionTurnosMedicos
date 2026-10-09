namespace ChronoSaludApi.Repositorios;

/// <summary>
/// Separa el texto de una búsqueda en palabras: "  ana   duarte " da
/// "ana" y "duarte". Cada palabra se busca por separado.
/// </summary>
public static class PalabrasDeBusqueda
{
    /// <summary>
    /// Más palabras no ayudan a encontrar a alguien y alargan la consulta:
    /// las que sobran se ignoran.
    /// </summary>
    public const int Maximo = 5;

    public static List<string> Separar(string? texto)
    {
        var palabras = new List<string>();
        if (string.IsNullOrWhiteSpace(texto))
            return palabras;

        // Sin separador indicado, Split corta en cualquier espacio (también tabs).
        foreach (var palabra in texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (palabras.Count == Maximo)
                break;

            palabras.Add(palabra);
        }

        return palabras;
    }
}
