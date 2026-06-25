namespace ChronoSaludApi.Entidades;

public class HistorialClinico
{
    public int Id { get; set; }

    public int IdPaciente { get; set; }

    public DateTime Fecha { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public string Diagnostico { get; set; } = string.Empty;

    public int? IdTurno { get; set; }

    // Navegación
    public Paciente? Paciente { get; set; }

    public Turno? Turno { get; set; }
}
