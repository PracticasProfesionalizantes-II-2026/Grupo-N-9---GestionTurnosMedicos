namespace ChronoSaludApi.Logica.DTOs;

public record HorarioLaboralDto(
    int DiaSemana,
    string HoraInicio,
    string HoraFin
);

public record HorarioSemanalDto(
    List<HorarioLaboralDto> Horarios
);

/// <summary>
/// Un turno reservado que el horario nuevo dejaría fuera. Lleva lo justo para
/// ubicarlo; nada clínico.
/// </summary>
public record TurnoEnConflictoDto(
    int IdTurno,
    DateTime FechaInicio,
    string HoraInicio,
    string Estado,
    string Paciente
);

public record FranjaDisponibleDto(
    string HoraInicio,
    string HoraFin
);

/// <summary>Un día y cuántas franjas libres tiene el doctor ese día.</summary>
public record DiaDisponibleDto(
    DateTime Fecha,
    int Libres
);
