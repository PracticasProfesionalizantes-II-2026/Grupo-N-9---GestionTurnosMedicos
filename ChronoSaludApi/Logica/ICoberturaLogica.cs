using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface ICoberturaLogica
{
    Task<IEnumerable<CoberturaDto>> ObtenerTodas(string? nombre);
    Task<IEnumerable<PacienteCoberturaDto>> ObtenerDePaciente(int pacienteId);
    Task<(bool ok, string? error)> AsociarAPaciente(int pacienteId, AsociarCoberturaDto dto);
    Task<(bool ok, string? error)> ActualizarDePaciente(int pacienteId, AsociarCoberturaDto dto);
    Task<(bool ok, string? error)> DesvincularDePaciente(int pacienteId, int coberturaId);
}
