namespace ChronoSaludApi.Logica.DTOs;

/// <summary>
/// Una fila del historial. FechaUtc va marcada como UTC. IdUsuario y Usuario
/// (nombre y apellido de quien actuó) solo se informan al administrador: al
/// doctor le llegan en null y ve únicamente el rol.
/// </summary>
public record MovimientoDto(
    int IdMovimiento,
    DateTime FechaUtc,
    string Accion,
    string Entidad,
    int IdEntidad,
    int? IdDoctor,
    string Resumen,
    string RolUsuario,
    int? IdUsuario,
    string? Usuario
);
