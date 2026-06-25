namespace ChronoSaludApi.Entidades;

public class RecetaMedicamento
{
    public int Id { get; set; }

    public int IdReceta { get; set; }

    public int IdMedicamento { get; set; }

    public string Dosis { get; set; } = string.Empty;

    public string Frecuencia { get; set; } = string.Empty;

    public string? Duracion { get; set; }

    public string? Indicaciones { get; set; }

    // Navegación
    public Receta? Receta { get; set; }

    public Medicamento? Medicamento { get; set; }
}
