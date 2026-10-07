using System.Globalization;

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

    // Un turno en estos estados sigue en pie: es el que no puede quedar fuera
    // del horario. Los completados y los cancelados ya no ocupan la agenda.
    private static readonly string[] EstadosReservados = ["pendiente", "confirmado"];

    private readonly ApiClient _api;
    private readonly DoctorService _doctores;
    private readonly TurnoService _turnos;

    public HorarioService(ApiClient api, DoctorService doctores, TurnoService turnos)
    {
        _api = api;
        _doctores = doctores;
        _turnos = turnos;
    }

    /// <summary>
    /// Único punto de la Web que escribe un horario: todo guardado pasa por acá.
    /// Primero revisa los turnos reservados de hoy en adelante; si el cambio
    /// dejaría alguno fuera de horario, no guarda y los devuelve.
    /// Si no, PUT /doctores/{id}/horarios reemplaza la semana entera por la
    /// lista que recibe: un día que no viene deja de atenderse, y la lista
    /// vacía deja al doctor sin horario. Reservado a administrador.
    /// Deja pasar la ApiException (400 si el doctor está inactivo o un rango es
    /// inválido, 404 si no existe, o la de no haber podido leer el horario
    /// actual) para que el controlador muestre el mensaje sin perder lo que se
    /// había cargado.
    /// </summary>
    public async Task<ResultadoGuardarHorario> GuardarAsync(int idDoctor, IReadOnlyList<HorarioLaboral> nuevo)
    {
        var conflictos = await BuscarConflictosAsync(idDoctor, nuevo);
        if (conflictos.Count > 0)
            return new ResultadoGuardarHorario(false, conflictos);

        await _api.PutAsync($"/doctores/{idDoctor}/horarios", new { horarios = nuevo });
        return new ResultadoGuardarHorario(true, Array.Empty<TurnoLista>());
    }

    /// <summary>
    /// Turnos pendientes o confirmados, de hoy en adelante, que entran en el
    /// horario que el doctor tiene hoy y dejarían de entrar en
    /// <paramref name="nuevo"/>. Uno que ya está fuera del horario actual no
    /// frena: no es este cambio el que lo deja ahí. La API no hace este
    /// control: su PUT reemplaza el horario sin mirar los turnos.
    /// </summary>
    private async Task<IReadOnlyList<TurnoLista>> BuscarConflictosAsync(int idDoctor, IReadOnlyList<HorarioLaboral> nuevo)
    {
        var actual = await LeerHorarioActualAsync(idDoctor);
        var turnos = await _turnos.ObtenerDeDoctorDesdeAsync(idDoctor, FechaArgentina.Hoy());

        return turnos
            .Where(t => EstadosReservados.Contains(t.Estado, StringComparer.OrdinalIgnoreCase))
            .Where(t => QuedaFuera(t, actual, nuevo))
            .OrderBy(t => t.FechaInicio)
            .ThenBy(t => t.HoraInicio, StringComparer.Ordinal)
            .ThenBy(t => t.IdTurno)
            .ToList();
    }

    /// <summary>
    /// El horario que el doctor tiene cargado hoy. Sin él no se sabe qué turnos
    /// deja fuera el cambio, así que si no se puede leer se corta con el
    /// motivo en lugar de guardar a ciegas.
    /// </summary>
    private async Task<IReadOnlyList<HorarioLaboral>> LeerHorarioActualAsync(int idDoctor)
    {
        const string motivo = "No se pudo leer el horario actual del doctor, así que no se guardó nada";

        IReadOnlyList<HorarioLaboral>? actual;
        try
        {
            actual = await _doctores.ObtenerHorariosAsync(idDoctor);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            throw new ApiException($"{motivo}: {error.Message}", error.Status);
        }

        // ObtenerHorariosAsync devuelve null cuando la API contesta 404.
        return actual ?? throw new ApiException($"{motivo}: el doctor no existe.", StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// El turno entra en el horario actual y dejaría de entrar en el nuevo.
    /// Uno sin hora, o con una que no se entiende, no se puede ubicar en
    /// ningún horario: no frena.
    /// </summary>
    private static bool QuedaFuera(
        TurnoLista turno, IReadOnlyList<HorarioLaboral> actual, IReadOnlyList<HorarioLaboral> nuevo)
    {
        if (!TryHora(turno.HoraInicio, out var inicio)) return false;

        var diaSemana = (int)turno.FechaInicio.DayOfWeek;
        return EntraEn(actual, diaSemana, inicio) && !EntraEn(nuevo, diaSemana, inicio);
    }

    /// <summary>
    /// La hora cae en un día que el horario atiende y dentro de su rango. El
    /// listado de turnos no trae la hora de fin, así que al turno se le supone
    /// la duración de una franja, que es lo único que reserva la Web.
    /// </summary>
    private static bool EntraEn(IReadOnlyList<HorarioLaboral> horario, int diaSemana, TimeSpan inicio)
    {
        var dia = horario.FirstOrDefault(h => h.DiaSemana == diaSemana);

        return dia is not null
            && TryHora(dia.HoraInicio, out var desde)
            && TryHora(dia.HoraFin, out var hasta)
            && inicio >= desde
            && inicio + TimeSpan.FromMinutes(MinutosPorFranja) <= hasta;
    }

    private static bool TryHora(string? texto, out TimeSpan hora) =>
        TimeSpan.TryParseExact(texto, @"hh\:mm", CultureInfo.InvariantCulture, out hora);
}
