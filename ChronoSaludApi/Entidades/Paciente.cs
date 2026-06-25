namespace ChronoSaludApi.Entidades;

public class Paciente
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public string? Sexo { get; set; }

    public string? GrupoSanguineo { get; set; }

    public string? Alergias { get; set; }

    public string? Condiciones { get; set; }

    // Navegación
    public Usuario? Usuario { get; set; }

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    public ICollection<PacienteCobertura> PacienteCoberturas { get; set; } = new List<PacienteCobertura>();

    public ICollection<HistorialClinico> HistorialesClinicos { get; set; } = new List<HistorialClinico>();

    public ICollection<Receta> Recetas { get; set; } = new List<Receta>();

    public ICollection<Estudio> Estudios { get; set; } = new List<Estudio>();
}
