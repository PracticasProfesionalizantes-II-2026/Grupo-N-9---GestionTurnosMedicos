using System.Text.Json;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Resultado de querer guardar un horario. Si el cambio dejaría turnos
/// reservados fuera de horario no se guarda nada y vienen en
/// <paramref name="Conflictos"/>, ordenados por fecha y hora.
/// </summary>
public record ResultadoGuardarHorario(bool Guardado, IReadOnlyList<TurnoLista> Conflictos);

/// <summary>
/// Escritura del horario semanal de un doctor. La lectura sigue en
/// DoctorService (ObtenerHorariosAsync), que es lo que usan las pantallas que
/// solo lo muestran.
/// </summary>
public class HorarioService
{
    /// <summary>
    /// Las franjas de turno duran 30 minutos y la API las cuenta desde la hora
    /// de inicio del horario (DuracionFranjaMinutos en HorarioLaboralLogica).
    /// </summary>
    public const int MinutosPorFranja = 30;

    private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);

    private readonly ApiClient _api;

    public HorarioService(ApiClient api) => _api = api;

    /// <summary>
    /// Único punto de la Web que escribe un horario: todo guardado pasa por acá.
    /// PUT /doctores/{id}/horarios reemplaza la semana entera por la lista que
    /// recibe: un día que no viene deja de atenderse, y la lista vacía deja al
    /// doctor sin horario. Reservado a administrador.
    /// El control de turnos lo hace la API: si el cambio dejaría fuera turnos
    /// pendientes o confirmados contesta 409 con la lista, y acá se devuelve
    /// sin guardar nada.
    /// Deja pasar el resto de las ApiException (400 si el doctor está inactivo
    /// o una hora es inválida, 404 si no existe, 503 si la base estaba
    /// ocupada) para que el controlador muestre el mensaje sin perder lo que
    /// se había cargado.
    /// </summary>
    public async Task<ResultadoGuardarHorario> GuardarAsync(int idDoctor, IReadOnlyList<HorarioLaboral> nuevo)
    {
        try
        {
            await _api.PutAsync($"/doctores/{idDoctor}/horarios", new { horarios = nuevo });
            return new ResultadoGuardarHorario(true, Array.Empty<TurnoLista>());
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status409Conflict
                                         && LeerConflictos(error.Cuerpo) is { Count: > 0 } conflictos)
        {
            return new ResultadoGuardarHorario(false, conflictos);
        }
    }

    /// <summary>
    /// La lista "conflictos" del cuerpo del 409. Vacía si el cuerpo no la trae
    /// o no se entiende: en ese caso el 409 sigue de largo como un error común
    /// y el controlador muestra su mensaje.
    /// </summary>
    private static IReadOnlyList<TurnoLista> LeerConflictos(string? cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo))
            return Array.Empty<TurnoLista>();

        try
        {
            var respuesta = JsonSerializer.Deserialize<RespuestaConConflictos>(cuerpo, JsonOpciones);

            return (respuesta?.Conflictos ?? new List<TurnoEnConflicto>())
                .Select(c => new TurnoLista(
                    c.IdTurno,
                    c.FechaInicio,
                    c.HoraInicio ?? string.Empty,
                    c.Estado ?? string.Empty,
                    Doctor: string.Empty,
                    Especialidad: string.Empty,
                    c.Paciente ?? string.Empty))
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<TurnoLista>();
        }
    }

    /// <summary>Cuerpo del 409 de PUT /doctores/{id}/horarios: { error, conflictos }.</summary>
    private sealed record RespuestaConConflictos(List<TurnoEnConflicto>? Conflictos);

    /// <summary>Espeja TurnoEnConflictoDto de la API.</summary>
    private sealed record TurnoEnConflicto(
        int IdTurno, DateTime FechaInicio, string? HoraInicio, string? Estado, string? Paciente);
}
