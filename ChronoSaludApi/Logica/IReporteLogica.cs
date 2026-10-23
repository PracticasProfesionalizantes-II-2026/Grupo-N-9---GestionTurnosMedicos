using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IReporteLogica
{
    /// <summary>
    /// <paramref name="desde"/> y <paramref name="hasta"/> son días, los dos
    /// incluidos. El administrador ve toda la clínica y puede filtrar por
    /// doctor y especialidad; el doctor ve solo sus turnos y los filtros se
    /// ignoran. <c>error</c> con <c>noEncontrado</c> en false es un pedido mal
    /// armado (400); con <c>noEncontrado</c>, un doctor que no existe o sin
    /// perfil (404). <c>prohibido</c> si el rol no ve reportes.
    /// </summary>
    Task<(ReporteResumenDto? reporte, string? error, bool noEncontrado, bool prohibido)> Resumen(
        DateTime? desde, DateTime? hasta, int? idDoctor, string? especialidad, Solicitante solicitante);
}
