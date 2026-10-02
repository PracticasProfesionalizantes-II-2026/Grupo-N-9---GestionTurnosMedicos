namespace ChronoSaludApi.Logica.DTOs;

public record MedicamentoDto(
    int IdMedicamento,
    string Nombre,
    string? Descripcion,
    string? NombreGenerico = null,
    string? Concentracion = null,
    string? FormaFarmaceutica = null,
    string? Laboratorio = null
);

public record MedicamentoCreateDto(
    string Nombre,
    string? Descripcion,
    string? NombreGenerico = null,
    string? Concentracion = null,
    string? FormaFarmaceutica = null,
    string? Laboratorio = null
);

public record RecetaMedicamentoDto(
    int IdMedicamento,
    string Dosis,
    string Frecuencia,
    string? Duracion,
    string? Indicaciones
);
