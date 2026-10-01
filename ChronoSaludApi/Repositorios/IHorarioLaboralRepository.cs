using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IHorarioLaboralRepository
{
    Task<IEnumerable<HorarioLaboral>> ObtenerPorDoctor(int idDoctor);
    Task<HorarioLaboral?> ObtenerPorDoctorYDia(int idDoctor, int diaSemana);
    Task ReemplazarHorarios(int idDoctor, IEnumerable<HorarioLaboral> nuevos);
}
