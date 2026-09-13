using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface ICoberturaLogica
{
    Task<IEnumerable<CoberturaDto>> ObtenerTodas(string? nombre);
    Task<(IEnumerable<PacienteCoberturaDto> coberturas, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, int idUsuarioCaller, bool callerEsStaff);
    Task<(bool ok, string? error, bool sinPerfil, bool prohibido)> AsociarAPaciente(
        int pacienteId, AsociarCoberturaDto dto, int idUsuarioCaller, bool callerEsPaciente, bool callerEsStaff);
    Task<(bool ok, string? error, bool sinPerfil, bool prohibido)> ActualizarDePaciente(
        int pacienteId, AsociarCoberturaDto dto, int idUsuarioCaller, bool callerEsPaciente, bool callerEsStaff);
    Task<(bool ok, string? error, bool sinPerfil, bool prohibido)> DesvincularDePaciente(
        int pacienteId, int coberturaId, int idUsuarioCaller, bool callerEsPaciente, bool callerEsStaff);
}
