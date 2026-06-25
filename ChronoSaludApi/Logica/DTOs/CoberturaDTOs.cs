namespace ChronoSaludApi.Logica.DTOs;

public record CoberturaDto(
    int IdCobertura,
    string Nombre,
    string? Plan
);

public record PacienteCoberturaDto(
    int Id,
    int IdCobertura,
    string NombreCobertura,
    string IdAfiliado,
    string? Plan
);

public record AsociarCoberturaDto(
    int IdCobertura,
    string IdAfiliado,
    string? Plan
);
