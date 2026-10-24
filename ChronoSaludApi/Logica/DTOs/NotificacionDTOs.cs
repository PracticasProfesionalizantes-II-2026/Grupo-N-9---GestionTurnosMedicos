namespace ChronoSaludApi.Logica.DTOs;

public record NotificacionDto(
    int Id,
    string Tipo,
    string Mensaje,
    DateTime Fecha,
    bool Leida,
    // Paso 23: a qué pantalla de la Web lleva; null si no lleva a una en particular.
    string? Enlace = null
);
