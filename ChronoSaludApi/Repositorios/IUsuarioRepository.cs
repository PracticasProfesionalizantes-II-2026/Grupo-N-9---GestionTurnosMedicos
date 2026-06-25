using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ObtenerTodos();
    Task<Usuario?> ObtenerPorId(int id);
    Task<Usuario?> ObtenerPorEmail(string email);
    Task Agregar(Usuario usuario);
    Task Actualizar(Usuario usuario);
    Task Eliminar(Usuario usuario);
}
