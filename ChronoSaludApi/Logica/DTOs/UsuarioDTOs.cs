namespace ChronoSaludApi.Logica.DTOs;

public record UsuarioDto(
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    string Rol
);

// Ficha: datos del paciente que se guardan junto con la cuenta. Solo se
// tienen en cuenta cuando un administrador da de alta a un paciente.
public record UsuarioRegistroDto(
    string Nombre,
    string Apellido,
    string Email,
    string Contrasena,
    string? Telefono,
    string Rol,
    PacienteUpdateDto? Ficha = null
);

public record UsuarioLoginDto(
    string Email,
    string Contrasena
);

public record UsuarioUpdateDto(
    string? Nombre,
    string? Apellido,
    string? Telefono,
    string? Contrasena
);

public record LoginResponseDto(
    string Token,
    string Rol,
    int IdUsuario,
    string Nombre
);

// IdPaciente viene cargado solo cuando la cuenta es de un paciente.
public record RegistroResponseDto(
    int IdUsuario,
    string Email,
    string Rol,
    string Token,
    int? IdPaciente = null
);

// Una fila del buscador de usuarios. IdPaciente e IdDoctor vienen cargados
// solo si el usuario tiene ese perfil.
public record UsuarioListaDto(
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    string Rol,
    bool Activo,
    int? IdPaciente,
    int? IdDoctor,
    bool TieneFoto
);
