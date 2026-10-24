using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IEstudioLogica
{
    Task<(IEnumerable<EstudioDto> estudios, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, string? tipo, string? estado, DateTime? desde, int idUsuarioCaller, bool callerEsStaff);
    Task<(EstudioDto? estudio, string? error, bool prohibido)> ObtenerPorId(int id, int idUsuarioCaller, bool callerEsStaff);
    Task<(int? id, string? error)> Crear(EstudioCreateDto dto, int idUsuarioCaller);
    /// <summary><paramref name="quien"/> es quien lo carga: si es el doctor que lo pidió, no se le avisa a él mismo.</summary>
    Task<(bool ok, string? error)> CargarResultado(int id, EstudioResultadoDto dto, Solicitante? quien = null);

    Task<(EstudioArchivo? archivo, string? enlace, string? error, bool prohibido)> ObtenerArchivo(int id, int idUsuarioCaller, bool callerEsStaff);
    Task<(int marcados, string? error)> MarcarVistos(int idUsuarioCaller);
    Task<(bool ok, string? error)> GuardarArchivo(int id, byte[] contenido, string? nombre);
    Task<(bool ok, string? error)> QuitarArchivo(int id);
}
