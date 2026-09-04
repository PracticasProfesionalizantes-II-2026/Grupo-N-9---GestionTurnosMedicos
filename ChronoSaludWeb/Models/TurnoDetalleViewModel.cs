namespace ChronoSaludWeb.Models;

public class TurnoDetalleViewModel
{
    public int IdTurno { get; init; }
    public DateTime FechaInicio { get; init; }
    public string HoraInicio { get; init; } = string.Empty;
    public string HoraFin { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string? Observaciones { get; init; }

    public int IdPaciente { get; init; }
    public int IdDoctor { get; init; }

    // Datos que el controlador resuelve contra /pacientes/{id} y /doctores/{id},
    // porque TurnoDto solo manda los IDs. Quedan en null si la API no los encontró.
    public string? PacienteNombre { get; init; }
    public string? DoctorNombre { get; init; }
    public string? Especialidad { get; init; }
    public string? Matricula { get; init; }
    public string? Consultorio { get; init; }

    /// <summary>Mensaje de error de la API, si la carga falló.</summary>
    public string? Error { get; init; }

    public bool HuboError => Error is not null;

    public string FechaLarga =>
        FechaInicio.ToString("D", TurnosIndexViewModel.Cultura);

    /// <summary>"10:00 a 10:30", o solo el inicio si la API no mandó el fin.</summary>
    public string Horario =>
        string.IsNullOrWhiteSpace(HoraFin) ? HoraInicio : $"{HoraInicio} a {HoraFin}";

    public string PacienteMostrado =>
        string.IsNullOrWhiteSpace(PacienteNombre) ? $"Paciente #{IdPaciente}" : PacienteNombre.Trim();

    public string DoctorMostrado =>
        string.IsNullOrWhiteSpace(DoctorNombre) ? $"Doctor #{IdDoctor}" : DoctorNombre.Trim();

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(PacienteMostrado);

    public bool TieneObservaciones => !string.IsNullOrWhiteSpace(Observaciones);
}
