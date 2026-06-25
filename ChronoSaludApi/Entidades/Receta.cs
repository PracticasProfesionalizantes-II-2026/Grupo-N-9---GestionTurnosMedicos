namespace ChronoSaludApi.Entidades;

public class Receta
{
    public int Id { get; set; }

    public int IdPaciente { get; set; }

    public int IdDoctor { get; set; }

    public int? IdTurno { get; set; }

    public DateTime Fecha { get; set; }

    public DateTime Vigencia { get; set; }

    public string? Detalles { get; set; }

    // Navegación
    public Paciente? Paciente { get; set; }

    public Doctor? Doctor { get; set; }

    public ICollection<RecetaMedicamento> RecetaMedicamentos { get; set; } = new List<RecetaMedicamento>();
}
