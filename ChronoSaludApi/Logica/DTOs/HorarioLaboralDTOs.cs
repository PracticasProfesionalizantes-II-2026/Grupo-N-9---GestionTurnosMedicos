namespace ChronoSaludApi.Logica.DTOs;

public record HorarioLaboralDto(
    int DiaSemana,
    string HoraInicio,
    string HoraFin
);

public record HorarioSemanalDto(
    List<HorarioLaboralDto> Horarios
);

public record FranjaDisponibleDto(
    string HoraInicio,
    string HoraFin
);
