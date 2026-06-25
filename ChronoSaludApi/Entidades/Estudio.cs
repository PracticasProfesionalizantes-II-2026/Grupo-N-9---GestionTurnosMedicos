namespace ChronoSaludApi.Entidades;

public class Estudio
{
    public int Id { get; set; }

    public int IdPaciente { get; set; }

    public int? IdTurno { get; set; }

    // "sangre" | "imagen" | "biopsia" | "otro"
    public string Tipo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaSolicitud { get; set; }

    // "pendiente" | "validado" | "entregado"
    public string Estado { get; set; } = "pendiente";

    public string? Resultado { get; set; }

    public string? ArchivoUrl { get; set; }

    public DateTime? FechaResultado { get; set; }

    // Navegación
    public Paciente? Paciente { get; set; }

    public Turno? Turno { get; set; }
}
