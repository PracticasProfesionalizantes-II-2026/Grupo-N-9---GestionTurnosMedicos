using ChronoSaludApi.Entidades;
using ChronoSaludApi.Repositorios;

namespace ChronoSalud.Tests.Falsos;

/// <summary>
/// Reemplaza a HorarioLaboralRepository. Los horarios viven en una lista y los
/// turnos se leen del TurnoRepositoryFalso que recibe, para que los dos
/// repositorios falsos vean los mismos datos.
/// </summary>
public class HorarioLaboralRepositoryFalso : IHorarioLaboralRepository
{
    private readonly TurnoRepositoryFalso _turnos;

    public HorarioLaboralRepositoryFalso(TurnoRepositoryFalso turnos)
    {
        _turnos = turnos;
    }

    public List<HorarioLaboral> Horarios { get; } = new List<HorarioLaboral>();

    public Task<IEnumerable<HorarioLaboral>> ObtenerPorDoctor(int idDoctor)
    {
        var delDoctor = Horarios.Where(h => h.IdDoctor == idDoctor).ToList();
        return Task.FromResult<IEnumerable<HorarioLaboral>>(delDoctor);
    }

    public Task<HorarioLaboral?> ObtenerPorDoctorYDia(int idDoctor, int diaSemana)
    {
        var horario = Horarios.FirstOrDefault(h => h.IdDoctor == idDoctor && h.DiaSemana == diaSemana);
        return Task.FromResult(horario);
    }

    public Task<(IReadOnlyList<HorarioLaboral> anteriores, IReadOnlyList<Turno> frenan)> ReemplazarHorarios(
        int idDoctor,
        IReadOnlyList<HorarioLaboral> nuevos,
        DateTime desde,
        Func<IReadOnlyList<HorarioLaboral>, IReadOnlyList<Turno>, IReadOnlyList<Turno>> turnosQueFrenan)
    {
        List<HorarioLaboral> actuales = Horarios.Where(h => h.IdDoctor == idDoctor).ToList();
        List<Turno> turnosDelDoctor = _turnos.Turnos.Where(t => t.IdDoctor == idDoctor && t.FechaInicio >= desde).ToList();

        // Qué turnos frenan el cambio lo decide la lógica (es la función que
        // recibimos). Acá, como en el repositorio real, solo se guarda si no
        // frena ninguno.
        IReadOnlyList<Turno> frenan = turnosQueFrenan(actuales, turnosDelDoctor);

        if (frenan.Count == 0)
        {
            Horarios.RemoveAll(h => h.IdDoctor == idDoctor);
            Horarios.AddRange(nuevos);
        }

        IReadOnlyList<HorarioLaboral> anteriores = actuales;
        return Task.FromResult((anteriores, frenan));
    }
}
