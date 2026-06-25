namespace ChronoSaludApi.Entidades;

public class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Apellido { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Contrasena { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    // "paciente" | "doctor" | "administrador"
    public string Rol { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    // Navegación
    public Paciente? Paciente { get; set; }

    public Doctor? Doctor { get; set; }

    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
}
