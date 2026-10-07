using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

/// <summary>
/// Historial de movimientos. A propósito no tiene actualizar ni eliminar: una
/// fila escrita no se toca más.
/// </summary>
public interface IMovimientoRepository
{
    Task Agregar(Movimiento movimiento);

    /// <summary>
    /// Del más nuevo al más viejo, con quien actuó cargado. El total y la
    /// página se resuelven en SQL. El rango es desde <paramref name="desdeUtc"/>
    /// inclusive hasta <paramref name="antesDeUtc"/> sin incluirlo.
    /// </summary>
    Task<(int total, IReadOnlyList<Movimiento> movimientos)> Buscar(
        int? idDoctor, string? accion, DateTime? desdeUtc, DateTime? antesDeUtc, int pagina, int limite);
}
