namespace ChronoSaludWeb.Models;

/// <summary>
/// Cómo se muestra el consultorio de un doctor. El dato se carga a mano, así
/// que a veces viene "101" y a veces "Consultorio 101": sin esto se leía
/// "Consultorio Consultorio 101", en pantalla y en el lector de pantalla.
/// </summary>
public static class ConsultorioTexto
{
    /// <summary>"Consultorio 101", agregue o no la palabra quien lo cargó. Null si no hay dato.</summary>
    public static string? Para(string? consultorio)
    {
        if (string.IsNullOrWhiteSpace(consultorio))
            return null;

        var texto = consultorio.Trim();
        return texto.StartsWith("consultorio", StringComparison.OrdinalIgnoreCase)
            ? texto
            : $"Consultorio {texto}";
    }
}
