using ChronoSaludApi.Logica.DTOs;

namespace ChronoSaludApi.Logica;

public interface IUsuarioLogica
{
    Task<(RegistroResponseDto? resultado, string? error)> Registrar(UsuarioRegistroDto dto);
    Task<(LoginResponseDto? resultado, string? error)> Login(UsuarioLoginDto dto);
    Task<UsuarioDto?> ObtenerPorId(int id);
    Task<(bool ok, string? error)> Actualizar(int id, UsuarioUpdateDto dto);
    Task<(bool ok, string? error)> EliminarLogico(int id);
}
