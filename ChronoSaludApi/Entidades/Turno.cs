namespace ChronoSaludApi.Entidades;

public class Turno
{
    public int Id { get; set; }

    public int IdPaciente { get; set; }

    public int IdDoctor { get; set; }

    public DateTime FechaInicio { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    // "pendiente" | "confirmado" | "cancelado" | "completado"
    public string Estado { get; set; } = "pendiente";

    public string? Observaciones { get; set; }

    // Navegación
    public Paciente? Paciente { get; set; }

    public Doctor? Doctor { get; set; }
}
