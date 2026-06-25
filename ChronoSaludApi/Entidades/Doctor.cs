namespace ChronoSaludApi.Entidades;

public class Doctor
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }

    public string Especialidad { get; set; } = string.Empty;

    public string Matricula { get; set; } = string.Empty;

    public string? Consultorio { get; set; }

    public bool Activo { get; set; } = true;

    // Navegación
    public Usuario? Usuario { get; set; }

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    public ICollection<Receta> Recetas { get; set; } = new List<Receta>();
}
