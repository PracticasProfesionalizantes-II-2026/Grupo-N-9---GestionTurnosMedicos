using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IMovimientoLogica
{
    /// <summary>
    /// <paramref name="desde"/> y <paramref name="hasta"/> son días de Argentina,
    /// los dos incluidos. <c>prohibido</c> si el rol no puede ver el historial; <c>error</c> si
    /// es un doctor sin perfil cargado.
    /// </summary>
    Task<(int total, IEnumerable<MovimientoDto> movimientos, string? error, bool prohibido)> Buscar(
        int? idDoctor, string? accion, DateTime? desde, DateTime? hasta, int pagina, int limite,
        Solicitante solicitante);
}
