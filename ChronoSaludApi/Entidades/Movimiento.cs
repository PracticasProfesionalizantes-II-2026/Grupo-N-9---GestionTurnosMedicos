namespace ChronoSaludApi.Entidades;

/// <summary>
/// Una fila del historial de movimientos: quién hizo qué y cuándo. Solo se
/// agrega; no se edita ni se borra. No guarda contenido clínico.
/// </summary>
public class Movimiento
{
    public int Id { get; set; }

    // Siempre en UTC.
    public DateTime FechaUtc { get; set; }

    public int IdUsuario { get; set; }

    // El rol que tenía el usuario en ese momento, aunque después cambie.
    public string RolUsuario { get; set; } = string.Empty;

    // Ver AccionesMovimiento: "horario.cambiado", "turno.creado", ...
    public string Accion { get; set; } = string.Empty;

    // "horario" | "turno"
    public string Entidad { get; set; } = string.Empty;

    // Id del turno, o del doctor cuando la entidad es su horario.
    public int IdEntidad { get; set; }

    // Doctor involucrado, para filtrar el historial por agenda.
    public int? IdDoctor { get; set; }

    // Texto corto para mostrar. Sin observaciones, diagnósticos ni nombres de pacientes.
    public string Resumen { get; set; } = string.Empty;

    // Navegación
    public Usuario? Usuario { get; set; }

    public Doctor? Doctor { get; set; }
}

/// <summary>Las acciones que registra el historial.</summary>
public static class AccionesMovimiento
{
    public const string HorarioCambiado   = "horario.cambiado";
    public const string TurnoCreado       = "turno.creado";
    public const string TurnoConfirmado   = "turno.confirmado";
    public const string TurnoCancelado    = "turno.cancelado";
    public const string TurnoCompletado   = "turno.completado";
    public const string TurnoAusente      = "turno.ausente";
    public const string TurnoReprogramado = "turno.reprogramado";

    // El turno pasó a un estado que no es confirmado, completado, ausente ni cancelado.
    public const string TurnoEstadoCambiado = "turno.estado_cambiado";

    public const int LargoMaximoDelResumen = 300;
}
