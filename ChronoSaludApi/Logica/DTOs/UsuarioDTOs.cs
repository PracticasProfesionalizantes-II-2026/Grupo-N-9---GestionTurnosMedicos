namespace ChronoSaludApi.Logica.DTOs;

public record UsuarioDto(
    int IdUsuario,
    string Nombre,
    string Apellido,
    string Email,
    string? Telefono,
    string Rol
);

public record UsuarioRegistroDto(
    string Nombre,
    string Apellido,
    string Email,
    string Contrasena,
    string? Telefono,
    string Rol
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

public record RegistroResponseDto(
    int IdUsuario,
    string Email,
    string Rol,
    string Token
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
