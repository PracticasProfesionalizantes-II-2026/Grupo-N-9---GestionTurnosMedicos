using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IUsuarioFotoLogica
{
    Task<(bool ok, string? error)> Guardar(int idUsuario, byte[] contenido);
    Task<UsuarioFotoDto?> Obtener(int idUsuario);
    Task<DateTime?> ObtenerFechaActualizacion(int idUsuario);
    Task<(bool ok, string? error)> Eliminar(int idUsuario);
    Task<DateTime?> ObtenerAcceso(int idUsuario, int idQuienPide, string? rolQuienPide);
    Task<FotosDisponiblesDto> ConsultarDisponibles(
        IEnumerable<int> idsPaciente, IEnumerable<int> idsDoctor, IEnumerable<int> idsTurno,
        int idQuienPide, string? rolQuienPide);
}
