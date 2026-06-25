namespace ChronoSaludApi.Entidades;

public class PacienteCobertura
{
    public int Id { get; set; }

    public int IdPaciente { get; set; }

    public int IdCobertura { get; set; }

    public string IdAfiliado { get; set; } = string.Empty;

    public string? Plan { get; set; }

    // Navegación
    public Paciente? Paciente { get; set; }

    public Cobertura? Cobertura { get; set; }
}
