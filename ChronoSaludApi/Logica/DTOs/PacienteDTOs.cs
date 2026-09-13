namespace ChronoSaludApi.Logica.DTOs;

public record PacienteDto(
    int IdPaciente,
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    DateTime? FechaNacimiento,
    string? Sexo,
    string? GrupoSanguineo,
    string? Alergias,
    string? Condiciones,
    string? Dni,
    string? Direccion,
    string? Nacionalidad,
    string? EstadoCivil,
    string? FotoUrl
);

public record PacienteListaDto(
    int IdPaciente,
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    string? Dni
);

public record PacienteUpdateDto(
    DateTime? FechaNacimiento,
    string? Sexo,
    string? GrupoSanguineo,
    string? Alergias,
    string? Condiciones,
    string? Dni,
    string? Direccion,
    string? Nacionalidad,
    string? EstadoCivil,
    string? FotoUrl
);
