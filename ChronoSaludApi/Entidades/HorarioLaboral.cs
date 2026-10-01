namespace ChronoSaludApi.Entidades;

public class HorarioLaboral
{
    public int Id { get; set; }

    public int IdDoctor { get; set; }

    // 0 = domingo ... 6 = sábado (mismo valor que (int)DayOfWeek)
    public int DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    // Navegación
    public Doctor? Doctor { get; set; }
}
