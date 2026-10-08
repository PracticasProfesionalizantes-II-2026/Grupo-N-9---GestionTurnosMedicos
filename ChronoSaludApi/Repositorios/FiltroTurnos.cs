namespace ChronoSaludApi.Repositorios;

/// <summary>
/// Los filtros del listado de turnos, juntos en una clase para no pasar seis
/// parámetros sueltos de un lado a otro. Lo que queda en null (o la lista
/// vacía) no filtra.
/// </summary>
public class FiltroTurnos
{
    public int? PacienteId { get; set; }
    public int? DoctorId { get; set; }

    /// <summary>Si tiene algo, solo los turnos en alguno de estos estados.</summary>
    public List<string> Estados { get; set; } = new List<string>();

    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    /// <summary>
    /// Los estados de un turno: los que cuenta el listado. Son los mismos que
    /// EstadosTurno.Todos (una prueba revisa que no se separen).
    /// </summary>
    public static readonly string[] EstadosConocidos = { "pendiente", "confirmado", "completado", "ausente", "cancelado" };
}
