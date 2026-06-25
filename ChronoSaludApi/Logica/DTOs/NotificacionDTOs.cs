namespace ChronoSaludApi.Logica.DTOs;

public record NotificacionDto(
    int Id,
    string Tipo,
    string Mensaje,
    DateTime Fecha,
    bool Leida
);
