using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IUsuarioFotoRepository
{
    Task<UsuarioFoto?> ObtenerPorIdUsuario(int idUsuario);
    Task<DateTime?> ObtenerFechaActualizacion(int idUsuario);
    Task Guardar(UsuarioFoto foto);
    Task Eliminar(int idUsuario);
}
