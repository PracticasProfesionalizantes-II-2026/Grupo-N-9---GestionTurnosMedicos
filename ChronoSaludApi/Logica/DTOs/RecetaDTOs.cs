namespace ChronoSaludApi.Logica.DTOs;

public record RecetaDto(
    int IdReceta,
    DateTime Fecha,
    DateTime Vigencia,
    string? Detalles,
    List<RecetaMedicamentoDto> Medicamentos
);

public record RecetaCreateDto(
    int IdPaciente,
    int IdDoctor,
    int? IdTurno,
    DateTime Fecha,
    DateTime Vigencia,
    string? Detalles,
    List<RecetaMedicamentoDto> Medicamentos
);
