using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

/// <summary>
/// La base no pudo completar la operación porque otra, en el mismo instante,
/// tenía tomadas las mismas filas (bloqueo o deadlock). No se guardó nada y
/// repetir el pedido es seguro.
/// </summary>
public class BaseOcupadaException : Exception
{
    public BaseOcupadaException(Exception interna)
        : base("La base de datos estaba ocupada con otra operación.", interna) { }
}

public interface IHorarioLaboralRepository
{
    Task<IEnumerable<HorarioLaboral>> ObtenerPorDoctor(int idDoctor);
    Task<HorarioLaboral?> ObtenerPorDoctorYDia(int idDoctor, int diaSemana);

    /// <summary>
    /// Reemplaza el horario del doctor si <paramref name="turnosQueFrenan"/>
    /// no encuentra ninguno. La función recibe el horario actual y los turnos
    /// del doctor desde <paramref name="desde"/>, y devuelve los que impiden el
    /// cambio. Lectura, control y guardado van en una sola transacción.
    /// Devuelve esos turnos (con el paciente cargado); vacío si guardó.
    /// Tira <see cref="BaseOcupadaException"/> si la base estaba bloqueada.
    /// </summary>
    Task<IReadOnlyList<Turno>> ReemplazarHorarios(
        int idDoctor,
        IReadOnlyList<HorarioLaboral> nuevos,
        DateTime desde,
        Func<IReadOnlyList<HorarioLaboral>, IReadOnlyList<Turno>, IReadOnlyList<Turno>> turnosQueFrenan);
}
