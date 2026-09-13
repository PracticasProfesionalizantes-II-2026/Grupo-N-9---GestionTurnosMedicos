using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IHistorialClinicoLogica
{
    Task<(int idPaciente, IEnumerable<HistorialClinicoDto> historiales, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, DateTime? desde, DateTime? hasta, int idUsuarioCaller, bool callerEsStaff);
    Task<(bool ok, string? error)> Crear(int pacienteId, int idUsuarioDoctor, HistorialClinicoCreateDto dto);
    Task<(bool ok, string? error, bool sinPerfilDoctor)> Actualizar(int pacienteId, int idHistorial, HistorialClinicoCreateDto dto, int idUsuarioDoctor);
}
