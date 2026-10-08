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
