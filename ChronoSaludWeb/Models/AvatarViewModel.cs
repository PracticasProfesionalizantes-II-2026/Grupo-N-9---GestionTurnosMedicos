using System.Globalization;

namespace ChronoSaludWeb.Models;

/// <summary>Los tres tamaños que ya usan las vistas: 9, 10 y 16 de la escala de Tailwind.</summary>
public enum TamanoAvatar
{
    Chico,
    Mediano,
    Grande
}

/// <summary>
/// Modelo del partial _Avatar. Recibe el nombre crudo (no "Sin datos" ni
/// "Paciente #12"): si no tiene de dónde sacar iniciales, el partial muestra
/// el ícono de persona.
/// </summary>
public class AvatarViewModel
{
    public string? Nombre { get; init; }

    public string? FotoUrl { get; init; }

    public TamanoAvatar Tamano { get; init; } = TamanoAvatar.Chico;

    public bool TieneFoto => !string.IsNullOrWhiteSpace(FotoUrl);

    /// <summary>
    /// "Pedro Paciente" -> "PP", "Pedro" -> "P". Toma la primera letra o dígito
    /// de la primera y de la última palabra; las palabras que no tienen ninguno
    /// no cuentan. Null si el nombre es nulo, vacío o no tiene letras ni dígitos.
    /// </summary>
    public string? Iniciales
    {
        get
        {
            var iniciales = (Nombre ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(PrimeraLetraODigito)
                .Where(inicial => inicial is not null)
                .ToList();

            if (iniciales.Count == 0) return null;

            var texto = iniciales.Count == 1
                ? iniciales[0]!
                : string.Concat(iniciales[0], iniciales[^1]);

            return texto.ToUpperInvariant();
        }
    }

    /// <summary>
    /// Recorre por elementos de texto y no por char, para no partir una letra
    /// con tilde combinada ni un carácter fuera del plano básico.
    /// </summary>
    private static string? PrimeraLetraODigito(string palabra)
    {
        var elementos = StringInfo.GetTextElementEnumerator(palabra);
        while (elementos.MoveNext())
        {
            var elemento = elementos.GetTextElement();
            if (char.IsLetterOrDigit(elemento, 0)) return elemento;
        }

        return null;
    }
}
