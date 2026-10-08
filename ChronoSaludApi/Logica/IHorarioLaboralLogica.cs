using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IHorarioLaboralLogica
{
    Task<(IEnumerable<HorarioLaboralDto>? horarios, string? error)> ObtenerPorDoctor(int idDoctor);
    /// <summary>
    /// Con <c>conflictos</c> cargado, el horario no se guardó porque dejaría
    /// esos turnos fuera. Con <c>reintentar</c>, la base estaba ocupada y no
    /// se guardó nada: se puede repetir el pedido. Con <c>prohibido</c>, el
    /// solicitante no puede cambiar ese horario.
    /// </summary>
    Task<(bool ok, string? error, IReadOnlyList<TurnoEnConflictoDto> conflictos, bool reintentar, bool prohibido)> Reemplazar(
        int idDoctor, HorarioSemanalDto dto, Solicitante solicitante);
    Task<(IEnumerable<FranjaDisponibleDto>? franjas, string? error)> ObtenerDisponibilidad(int idDoctor, DateTime fecha);
    Task<IEnumerable<string>> ObtenerEspecialidades();
    Task<(bool ok, string? error)> ValidarHorarioLaboral(int idDoctor, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin);
}
