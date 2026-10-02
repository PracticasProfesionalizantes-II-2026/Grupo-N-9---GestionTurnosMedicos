namespace ChronoSaludApi.Entidades;

public class Medicamento
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    // Datos del Vademécum Nacional. El nombre comercial va en Nombre.
    public string? NombreGenerico { get; set; }

    public string? Concentracion { get; set; }

    public string? FormaFarmaceutica { get; set; }

    public string? Laboratorio { get; set; }

    // Navegación
    public ICollection<RecetaMedicamento> RecetaMedicamentos { get; set; } = new List<RecetaMedicamento>();
}
