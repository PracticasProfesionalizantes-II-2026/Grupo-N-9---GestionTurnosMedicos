namespace ChronoSaludApi.Entidades;

public class Cobertura
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Plan { get; set; }

    // Navegación
    public ICollection<PacienteCobertura> PacienteCoberturas { get; set; } = new List<PacienteCobertura>();
}
