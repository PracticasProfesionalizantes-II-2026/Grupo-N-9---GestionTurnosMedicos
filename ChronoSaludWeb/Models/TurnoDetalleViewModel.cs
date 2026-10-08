using System.Globalization;
using ChronoSaludWeb.Services;

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

    /// <summary>Foto del paciente. Null si no tiene: el avatar muestra las iniciales.</summary>
    public string? PacienteFotoUrl { get; init; }

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

    /// <summary>
    /// La hora de ahora en Argentina. Se toma una sola vez; las pruebas pueden
    /// poner otra para probar un turno que ya empezó o que todavía no.
    /// </summary>
    public DateTime Ahora { get; init; } = FechaArgentina.Ahora();

    /// <summary>
    /// Ya llegó la hora de inicio del turno. Sin hora (o con una que no se
    /// entiende) se toma el comienzo del día.
    /// </summary>
    public bool YaEmpezo
    {
        get
        {
            var inicio = FechaInicio.Date;
            if (TimeSpan.TryParseExact(HoraInicio, @"hh\:mm", CultureInfo.InvariantCulture, out var hora))
                inicio = inicio + hora;

            return Ahora >= inicio;
        }
    }

    // Estados del dominio (Turno.Estado en la API): pendiente, confirmado,
    // completado, ausente y cancelado, siempre en minúscula. Se comparan sin
    // distinguir mayúsculas por si en la base quedó alguno escrito distinto.
    private bool EstadoEs(string otro) =>
        string.Equals(Estado, otro, StringComparison.OrdinalIgnoreCase);

    /// <summary>Pendiente o confirmado: el turno todavía puede pasar.</summary>
    public bool EstaEnPie => EstadoEs("pendiente") || EstadoEs("confirmado");

    /// <summary>Completado, ausente y cancelado son finales: de ahí el turno no se mueve más.</summary>
    public bool EsTerminal => !EstaEnPie;

    public bool PuedeConfirmarse => EstadoEs("pendiente");

    /// <summary>
    /// La API solo cancela un turno pendiente o confirmado (DELETE /turnos/{id}
    /// contesta 409 con cualquier otro estado).
    /// </summary>
    public bool PuedeCancelarse => EstaEnPie;

    /// <summary>Completado o ausente: solo un turno en pie que ya empezó (la API pide lo mismo).</summary>
    public bool PuedeCompletarse => EstaEnPie && YaEmpezo;

    public bool PuedeMarcarAusente => EstaEnPie && YaEmpezo;
}
