namespace ChronoSaludApi.Logica.DTOs;

public record HistorialClinicoDto(
    int IdHistorial,
    DateTime Fecha,
    string Descripcion,
    string Diagnostico,
    int? IdTurno
);

public record HistorialClinicoCreateDto(
    DateTime Fecha,
    string Descripcion,
    string Diagnostico,
    int? IdTurno
);
