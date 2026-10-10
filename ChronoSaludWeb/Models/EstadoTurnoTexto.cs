namespace ChronoSaludWeb.Models;

/// <summary>
/// Cómo se nombra el estado de un turno en pantalla. La API manda palabras
/// técnicas en minúscula ("pendiente", "completado"); el personal las conoce y
/// le sirven cortas para las listas, pero al paciente se le dice qué significan.
/// </summary>
public static class EstadoTurnoTexto
{
    public static string Para(string? estado, bool paciente)
    {
        var texto = (estado ?? string.Empty).Trim().ToLowerInvariant();

        if (paciente)
        {
            switch (texto)
            {
                case "pendiente":  return "Esperando confirmación";
                case "completado": return "Atendido";
            }
        }

        // El resto se entiende igual: solo va con la primera letra en mayúscula.
        return texto.Length == 0 ? "Sin estado" : char.ToUpperInvariant(texto[0]) + texto[1..];
    }
}
