using ChronoSaludApi.Logica;

namespace ChronoSaludApi.Entidades;

public class Notificacion
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }

    // "turno" | "estudio" | "receta" | "general"
    public string Tipo { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    // Hora de Argentina: DateTime.Now sería la del servidor (UTC en Azure).
    public DateTime Fecha { get; set; } = FechaArgentina.Ahora();

    public bool Leida { get; set; } = false;

    // Navegación
    public Usuario? Usuario { get; set; }
}
