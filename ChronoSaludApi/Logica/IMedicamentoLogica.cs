using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IMedicamentoLogica
{
    Task<IEnumerable<MedicamentoDto>> ObtenerTodos();
    Task<MedicamentoDto?> ObtenerPorId(int id);
    Task<int> Crear(MedicamentoCreateDto dto);
    Task<bool> Actualizar(int id, MedicamentoCreateDto dto);
    Task<bool> Eliminar(int id);
}
