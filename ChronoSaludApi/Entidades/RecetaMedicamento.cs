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

    // Copia de los datos del medicamento al momento de emitir la receta. Así
    // la receta no cambia si después se edita el medicamento. Las recetas
    // emitidas antes de este cambio pueden tenerla vacía.
    public string? NombreMedicamento { get; set; }

    public string? NombreGenerico { get; set; }

    public string? Concentracion { get; set; }

    public string? FormaFarmaceutica { get; set; }

    // Navegación
    public Receta? Receta { get; set; }

    public Medicamento? Medicamento { get; set; }
}
