using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSalud.Tests.Falsos;

/// <summary>
/// Reemplaza a HorarioLaboralLogica en las pruebas de turnos. Por defecto dice
/// que todo horario es válido; si se carga <see cref="ErrorDeHorario"/>, lo
/// rechaza con ese mensaje. El resto de los métodos no se usa en esas pruebas.
/// </summary>
public class HorarioLaboralLogicaFalsa : IHorarioLaboralLogica
{
    public string? ErrorDeHorario { get; set; }

    public Task<(bool ok, string? error)> ValidarHorarioLaboral(int idDoctor, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin)
    {
        if (ErrorDeHorario != null)
            return Task.FromResult<(bool, string?)>((false, ErrorDeHorario));

        return Task.FromResult<(bool, string?)>((true, null));
    }

    public Task<(IEnumerable<HorarioLaboralDto>? horarios, string? error)> ObtenerPorDoctor(int idDoctor)
        => throw new NotImplementedException("No se usa en estas pruebas.");

    public Task<(bool ok, string? error, IReadOnlyList<TurnoEnConflictoDto> conflictos, bool reintentar, bool prohibido)> Reemplazar(
        int idDoctor, HorarioSemanalDto dto, Solicitante solicitante)
        => throw new NotImplementedException("No se usa en estas pruebas.");

    public Task<(IEnumerable<FranjaDisponibleDto>? franjas, string? error)> ObtenerDisponibilidad(int idDoctor, DateTime fecha)
        => throw new NotImplementedException("No se usa en estas pruebas.");

    public Task<IEnumerable<string>> ObtenerEspecialidades()
        => throw new NotImplementedException("No se usa en estas pruebas.");
}
