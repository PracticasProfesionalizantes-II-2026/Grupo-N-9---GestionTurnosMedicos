namespace ChronoSaludApi.Logica.DTOs;

public record PacienteDto(
    int IdPaciente,
    string Nombre,
    string Apellido,
    DateTime? FechaNacimiento,
    string? Sexo,
    string? GrupoSanguineo,
    string? Alergias,
    string? Condiciones
);

public record PacienteListaDto(
    int IdPaciente,
    string Nombre,
    string Apellido
);

public record PacienteUpdateDto(
    DateTime? FechaNacimiento,
    string? Sexo,
    string? GrupoSanguineo,
    string? Alergias,
    string? Condiciones
);
