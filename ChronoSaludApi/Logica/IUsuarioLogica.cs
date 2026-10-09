using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IUsuarioLogica
{
    Task<(RegistroResponseDto? resultado, string? error, bool sinPermiso)> Registrar(
        UsuarioRegistroDto dto, bool esAdministrador);
    Task<(LoginResponseDto? resultado, string? error)> Login(UsuarioLoginDto dto);
    Task<UsuarioDto?> ObtenerPorId(int id);
    Task<(bool ok, string? error)> Actualizar(int id, UsuarioUpdateDto dto);
    Task<(bool ok, string? error)> CambiarContrasena(int idUsuario, CambioContrasenaDto dto);
    Task<(bool ok, string? error)> DarDeBaja(int id, int idSolicitante);
    Task<(bool ok, string? error)> Reactivar(int id);
    Task<(int total, IEnumerable<UsuarioListaDto> usuarios, string? error)> Buscar(
        string? buscar, string? rol, int pagina, int limite, bool bajas = false);
}
