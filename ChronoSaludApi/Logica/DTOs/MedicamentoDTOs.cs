namespace ChronoSaludApi.Logica.DTOs;

public record MedicamentoDto(
    int IdMedicamento,
    string Nombre,
    string? Descripcion
);

public record MedicamentoCreateDto(
    string Nombre,
    string? Descripcion
);

public record RecetaMedicamentoDto(
    int IdMedicamento,
    string Dosis,
    string Frecuencia,
    string? Duracion,
    string? Indicaciones
);
