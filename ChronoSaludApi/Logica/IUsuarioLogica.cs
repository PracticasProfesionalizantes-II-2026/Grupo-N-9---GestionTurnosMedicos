using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IUsuarioLogica
{
    Task<(RegistroResponseDto? resultado, string? error, bool sinPermiso)> Registrar(
        UsuarioRegistroDto dto, bool esAdministrador);
    Task<(LoginResponseDto? resultado, string? error)> Login(UsuarioLoginDto dto);
    Task<UsuarioDto?> ObtenerPorId(int id);
    Task<(bool ok, string? error)> Actualizar(int id, UsuarioUpdateDto dto);
    Task<(bool ok, string? error)> EliminarLogico(int id);
    Task<(int total, IEnumerable<UsuarioListaDto> usuarios, string? error)> Buscar(
        string? buscar, string? rol, int pagina, int limite);
}
