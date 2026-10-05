namespace ChronoSaludWeb.Models;

/// <summary>
/// Color de un chip. Cada tono pasa 4,5:1 como texto sobre su propio fondo
/// (ver la tabla de docs/diseno.md); el significado lo lleva siempre el texto,
/// el color solo acompaña.
/// </summary>
public enum TonoChip
{
    /// <summary>Dato sin carga de estado: un rol, "vencida", "fuera del vademécum".</summary>
    Neutro,
    /// <summary>Verde: confirmado, vigente.</summary>
    Exito,
    /// <summary>Ámbar: pendiente.</summary>
    Aviso,
    /// <summary>Azul: completado.</summary>
    Info,
    /// <summary>Rojo: cancelado.</summary>
    Peligro,
    /// <summary>Beige: la etiqueta de lo que todavía no está disponible.</summary>
    Proximamente
}

/// <summary>Modelo del partial _Chip.</summary>
public class ChipViewModel
{
    public required string Texto { get; init; }

    public TonoChip Tono { get; init; } = TonoChip.Neutro;

    /// <summary>
    /// Pone en mayúscula la primera letra. Para textos que llegan en minúscula
    /// desde la API ("pendiente", "administrador").
    /// </summary>
    public bool Capitalizar { get; init; }
}
