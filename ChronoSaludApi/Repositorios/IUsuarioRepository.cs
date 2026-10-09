using ChronoSaludApi.Entidades;

namespace ChronoSaludApi.Repositorios;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ObtenerTodos();
    Task<Usuario?> ObtenerPorId(int id);
    Task<Usuario?> ObtenerConDoctor(int id);
    Task<Usuario?> ObtenerPorEmail(string email);
    Task<bool> HayAdministradorActivo();
    Task<int> ContarAdministradoresActivos();
    Task<bool> EstaActivo(int id);
    Task Agregar(Usuario usuario);
    Task Actualizar(Usuario usuario);
    Task GuardarActivo(Usuario usuario);
    Task<(int total, IEnumerable<Usuario> usuarios)> Buscar(string? buscar, string? rol, int pagina, int limite, bool bajas = false);
    Task<IEnumerable<int>> ObtenerIdsConFoto(IEnumerable<int> idsUsuario);
}
