using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IDoctorLogica
{
    Task<(int total, IEnumerable<DoctorListaDto> doctores)> ObtenerTodos(string? especialidad, int? coberturaId, int pagina, int limite);
    Task<DoctorDto?> ObtenerPorId(int id);
    Task<(int? id, string? error)> Crear(DoctorCreateDto dto);
    Task<(bool ok, string? error)> Actualizar(int id, DoctorUpdateDto dto);
    Task<(bool ok, string? error)> EliminarLogico(int id);
}
