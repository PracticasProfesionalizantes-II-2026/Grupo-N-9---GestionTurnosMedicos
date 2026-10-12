namespace ChronoSaludWeb.Models;

/// <summary>
/// La clase de la franja de color (app.css, .franja-*) que acompaña al estado
/// de un turno en una fila o tarjeta. Sigue el mismo criterio que el chip de
/// _BadgeEstado; el que dice el estado con palabras es el chip.
/// </summary>
public static class FranjaEstado
{
    public static string Clase(string? estado) => estado?.ToLowerInvariant() switch
    {
        "confirmado" => "franja franja-exito",
        "pendiente"  => "franja franja-aviso",
        "completado" => "franja franja-info",
        "cancelado"  => "franja franja-peligro",
        _            => "franja franja-neutro",
    };
}
