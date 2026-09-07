namespace ChronoSaludApi.Logica.DTOs;

public record HistorialClinicoDto(
    int IdHistorial,
    DateTime Fecha,
    int IdDoctor,
    string Descripcion,
    string Diagnostico,
    int? IdTurno
);

// IdDoctor no viaja en el cuerpo: sale del claim del token del doctor autenticado.
public record HistorialClinicoCreateDto(
    DateTime Fecha,
    string Descripcion,
    string Diagnostico,
    int? IdTurno
);
