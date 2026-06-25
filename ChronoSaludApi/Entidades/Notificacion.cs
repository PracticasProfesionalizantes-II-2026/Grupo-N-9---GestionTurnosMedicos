namespace ChronoSaludApi.Entidades;

public class Notificacion
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }

    // "turno" | "estudio" | "receta" | "general"
    public string Tipo { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.Now;

    public bool Leida { get; set; } = false;

    // Navegación
    public Usuario? Usuario { get; set; }
}
