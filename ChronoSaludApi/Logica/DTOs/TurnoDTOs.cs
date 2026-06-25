namespace ChronoSaludApi.Logica.DTOs;

public record TurnoDto(
    int IdTurno,
    DateTime FechaInicio,
    string HoraInicio,
    string HoraFin,
    string Estado,
    int IdPaciente,
    int IdDoctor,
    string? Observaciones
);

public record TurnoListaDto(
    int IdTurno,
    DateTime FechaInicio,
    string Estado,
    string Doctor,
    string Paciente
);

public record TurnoCreateDto(
    int IdPaciente,
    int IdDoctor,
    DateTime FechaInicio,
    string HoraInicio,
    string HoraFin,
    string? Observaciones
);

public record TurnoUpdateDto(
    DateTime? FechaInicio,
    string? HoraInicio,
    string? HoraFin,
    string? Estado,
    string? Observaciones
);
