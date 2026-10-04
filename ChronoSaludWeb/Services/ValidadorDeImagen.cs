namespace ChronoSaludWeb.Services;

/// <summary>
/// Una foto ya validada: los bytes y el tipo que surge de su firma.
/// </summary>
public record ImagenValidada(byte[] Contenido, string TipoContenido);

/// <summary>
/// Valida una foto subida por formulario antes de mandarla a la API, que
/// vuelve a aplicar las mismas reglas por su cuenta. No mira la extensión ni
/// el Content-Type que declara el navegador: el tipo sale de los primeros
/// bytes del archivo.
/// </summary>
public static class ValidadorDeImagen
{
    /// <summary>2 MB.</summary>
    public const long TamanoMaximo = 2 * 1024 * 1024;

    public const string FormatosAceptados = "JPG, PNG o WebP";

    /// <summary>
    /// Devuelve la imagen lista para enviar, o el mensaje de error para
    /// mostrarle a la persona. Exactamente uno de los dos viene cargado.
    /// </summary>
    public static async Task<(ImagenValidada? imagen, string? error)> ValidarAsync(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return (null, "Elegí una foto.");

        if (archivo.Length > TamanoMaximo)
            return (null, "La foto no puede superar los 2 MB.");

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);
        var contenido = memoria.ToArray();

        var tipo = DetectarTipo(contenido);
        return tipo is null
            ? (null, $"El archivo no es una imagen válida ({FormatosAceptados}).")
            : (new ImagenValidada(contenido, tipo), null);
    }

    /// <summary>
    /// El tipo según la firma, o null si no es JPEG, PNG ni WebP. SVG, GIF,
    /// PDF o un texto renombrado a .jpg caen en null.
    /// </summary>
    public static string? DetectarTipo(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> jpeg = [0xFF, 0xD8, 0xFF];
        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        if (bytes.StartsWith(jpeg)) return "image/jpeg";
        if (bytes.StartsWith(png)) return "image/png";

        // WebP: "RIFF", cuatro bytes de tamaño y "WEBP".
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }
}
