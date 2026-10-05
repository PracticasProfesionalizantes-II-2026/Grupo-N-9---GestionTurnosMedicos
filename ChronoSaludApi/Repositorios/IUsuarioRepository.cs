using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ObtenerTodos();
    Task<Usuario?> ObtenerPorId(int id);
    Task<Usuario?> ObtenerPorEmail(string email);
    Task<bool> HayAdministradorActivo();
    Task Agregar(Usuario usuario);
    Task Actualizar(Usuario usuario);
    Task Eliminar(Usuario usuario);
    Task<(int total, IEnumerable<Usuario> usuarios)> Buscar(string? buscar, string? rol, int pagina, int limite);
    Task<IEnumerable<int>> ObtenerIdsConFoto(IEnumerable<int> idsUsuario);
}
