using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class UsuarioLogica : IUsuarioLogica
{
    private readonly IUsuarioRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IConfiguration _config;

    public UsuarioLogica(IUsuarioRepository repo, IPacienteRepository pacienteRepo, IConfiguration config)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _config = config;
    }

    public async Task<(RegistroResponseDto? resultado, string? error)> Registrar(UsuarioRegistroDto dto)
    {
        var existente = await _repo.ObtenerPorEmail(dto.Email);
        if (existente != null)
            return (null, "El email ya se encuentra registrado en el sistema.");

        var rolesValidos = new[] { "paciente", "doctor", "administrador" };
        if (!rolesValidos.Contains(dto.Rol))
            return (null, "Rol inválido. Valores válidos: paciente, doctor, administrador.");

        var usuario = new Usuario
        {
            Nombre     = dto.Nombre,
            Apellido   = dto.Apellido,
            Email      = dto.Email,
            Contrasena = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena),
            Telefono   = dto.Telefono,
            Rol        = dto.Rol,
            Activo     = true
        };

        await _repo.Agregar(usuario);

        if (usuario.Rol == "paciente")
            await _pacienteRepo.Agregar(new Paciente { IdUsuario = usuario.Id });

        var token = GenerarToken(usuario);

        return (new RegistroResponseDto(usuario.Id, usuario.Email, usuario.Rol, token), null);
    }

    public async Task<(LoginResponseDto? resultado, string? error)> Login(UsuarioLoginDto dto)
    {
        var usuario = await _repo.ObtenerPorEmail(dto.Email);
        if (usuario == null || !usuario.Activo)
            return (null, "Credenciales incorrectas.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Contrasena, usuario.Contrasena))
            return (null, "Credenciales incorrectas.");

        var token = GenerarToken(usuario);

        return (new LoginResponseDto(token, usuario.Rol, usuario.Id, usuario.Nombre), null);
    }

    public async Task<UsuarioDto?> ObtenerPorId(int id)
    {
        var u = await _repo.ObtenerPorId(id);
        if (u == null) return null;

        return new UsuarioDto(u.Id, u.Nombre, u.Apellido, u.Email, u.Telefono, u.Rol);
    }

    public async Task<(bool ok, string? error)> Actualizar(int id, UsuarioUpdateDto dto)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null) return (false, "Usuario no encontrado.");

        if (!string.IsNullOrEmpty(dto.Nombre))    usuario.Nombre    = dto.Nombre;
        if (!string.IsNullOrEmpty(dto.Apellido))  usuario.Apellido  = dto.Apellido;
        if (!string.IsNullOrEmpty(dto.Telefono))  usuario.Telefono  = dto.Telefono;
        if (!string.IsNullOrEmpty(dto.Contrasena))
            usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena);

        await _repo.Actualizar(usuario);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> EliminarLogico(int id)
    {
        var usuario = await _repo.ObtenerPorId(id);
        if (usuario == null) return (false, "Usuario no encontrado.");

        usuario.Activo = false;
        await _repo.Eliminar(usuario);
        return (true, null);
    }

    private string GenerarToken(Usuario usuario)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
