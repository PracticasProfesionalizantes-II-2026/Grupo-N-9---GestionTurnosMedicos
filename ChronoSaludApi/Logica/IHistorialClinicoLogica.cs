using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IHistorialClinicoLogica
{
    Task<(int idPaciente, IEnumerable<HistorialClinicoDto> historiales)?> ObtenerDePaciente(int pacienteId, DateTime? desde, DateTime? hasta);
    Task<(bool ok, string? error)> Crear(int pacienteId, HistorialClinicoCreateDto dto);
    Task<(bool ok, string? error)> Actualizar(int pacienteId, HistorialClinicoCreateDto dto);
}
