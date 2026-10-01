using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IHorarioLaboralLogica
{
    Task<(IEnumerable<HorarioLaboralDto>? horarios, string? error)> ObtenerPorDoctor(int idDoctor);
    Task<(bool ok, string? error)> Reemplazar(int idDoctor, HorarioSemanalDto dto);
    Task<(IEnumerable<FranjaDisponibleDto>? franjas, string? error)> ObtenerDisponibilidad(int idDoctor, DateTime fecha);
    Task<IEnumerable<string>> ObtenerEspecialidades();
    Task<(bool ok, string? error)> ValidarHorarioLaboral(int idDoctor, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin);
}
