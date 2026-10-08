using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>
/// Reemplaza a TurnoRepository en las pruebas: guarda los turnos en una lista
/// en memoria en vez de ir a la base de datos.
/// </summary>
public class TurnoRepositoryFalso : ITurnoRepository
{
    public List<Turno> Turnos { get; } = new List<Turno>();

    public Task<IEnumerable<Turno>> ObtenerTodos(int? pacienteId, int? doctorId, string? estado, DateTime? desde, DateTime? hasta)
    {
        var resultado = new List<Turno>();

        foreach (var turno in Turnos)
        {
            if (pacienteId.HasValue && turno.IdPaciente != pacienteId.Value) continue;
            if (doctorId.HasValue && turno.IdDoctor != doctorId.Value) continue;
            if (!string.IsNullOrEmpty(estado) && turno.Estado != estado) continue;
            if (desde.HasValue && turno.FechaInicio < desde.Value) continue;
            if (hasta.HasValue && turno.FechaInicio > hasta.Value) continue;

            resultado.Add(turno);
        }

        return Task.FromResult<IEnumerable<Turno>>(resultado);
    }

    /// <summary>
    /// Filtra y pagina la lista. El orden es solo por fecha, hora e Id: el
    /// resto del orden lo resuelve SQL Server y se prueba a mano.
    /// </summary>
    public Task<(int total, List<Turno> turnos)> Buscar(
        FiltroTurnos filtro, string orden, bool descendente, int pagina, int limite)
    {
        var filtrados = Filtrar(filtro, usarEstados: true)
            .OrderBy(t => t.FechaInicio)
            .ThenBy(t => t.HoraInicio)
            .ThenBy(t => t.Id)
            .ToList();

        var deLaPagina = filtrados.Skip((pagina - 1) * limite).Take(limite).ToList();
        return Task.FromResult((filtrados.Count, deLaPagina));
    }

    public Task<Dictionary<string, int>> ContarPorEstado(FiltroTurnos filtro)
    {
        var conteos = new Dictionary<string, int>();
        var filtrados = Filtrar(filtro, usarEstados: false);

        foreach (var estado in FiltroTurnos.EstadosConocidos)
        {
            conteos[estado] = filtrados.Count(t => t.Estado == estado);
        }

        return Task.FromResult(conteos);
    }

    public Task<int> ContarPacientesAtendidos(DateTime desde, DateTime hasta)
    {
        var pacientes = Turnos
            .Where(t => t.Estado == "completado" && t.FechaInicio >= desde && t.FechaInicio <= hasta)
            .Select(t => t.IdPaciente)
            .Distinct()
            .Count();

        return Task.FromResult(pacientes);
    }

    public Task<Dictionary<int, int>> ContarOcupadosPorDoctor(int? doctorId, DateTime desde, DateTime hasta)
    {
        var resultado = new Dictionary<int, int>();
        var filtro = new FiltroTurnos { DoctorId = doctorId, Desde = desde, Hasta = hasta };

        foreach (var turno in Filtrar(filtro, usarEstados: false))
        {
            if (turno.Estado == "cancelado") continue;

            resultado.TryGetValue(turno.IdDoctor, out var cantidad);
            resultado[turno.IdDoctor] = cantidad + 1;
        }

        return Task.FromResult(resultado);
    }

    private List<Turno> Filtrar(FiltroTurnos filtro, bool usarEstados)
    {
        var resultado = new List<Turno>();

        foreach (var turno in Turnos)
        {
            if (filtro.PacienteId.HasValue && turno.IdPaciente != filtro.PacienteId.Value) continue;
            if (filtro.DoctorId.HasValue && turno.IdDoctor != filtro.DoctorId.Value) continue;
            if (filtro.Desde.HasValue && turno.FechaInicio < filtro.Desde.Value) continue;
            if (filtro.Hasta.HasValue && turno.FechaInicio > filtro.Hasta.Value) continue;
            if (usarEstados && filtro.Estados.Count > 0 && !filtro.Estados.Contains(turno.Estado)) continue;

            resultado.Add(turno);
        }

        return resultado;
    }

    public Task<Turno?> ObtenerPorId(int id)
    {
        var turno = Turnos.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(turno);
    }

    public Task<bool> HayConflictoHorario(int doctorId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin, int? turnoId = null)
    {
        foreach (var turno in Turnos)
        {
            var mismoDoctorYDia = turno.IdDoctor == doctorId && turno.FechaInicio.Date == fecha.Date;
            var esOtroTurno = turnoId == null || turno.Id != turnoId;
            var seSuperponen = turno.HoraInicio < horaFin && turno.HoraFin > horaInicio;

            if (mismoDoctorYDia && esOtroTurno && turno.Estado != "cancelado" && seSuperponen)
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task Agregar(Turno turno)
    {
        // Igual que la base de datos: el id lo asigna quien guarda.
        var mayor = 0;
        foreach (var existente in Turnos)
        {
            if (existente.Id > mayor) mayor = existente.Id;
        }

        turno.Id = mayor + 1;
        Turnos.Add(turno);
        return Task.CompletedTask;
    }

    // Los turnos de la lista son los mismos objetos que modifica la lógica,
    // así que actualizar no tiene nada más que hacer.
    public Task Actualizar(Turno turno) => Task.CompletedTask;

    public Task Eliminar(Turno turno) => Task.CompletedTask;
}
