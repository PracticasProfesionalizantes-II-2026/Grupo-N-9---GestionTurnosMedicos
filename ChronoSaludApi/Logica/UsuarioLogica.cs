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

    public async Task<(RegistroResponseDto? resultado, string? error, bool sinPermiso)> Registrar(
        UsuarioRegistroDto dto, bool esAdministrador)
    {
        var rolesValidos = new[] { "paciente", "doctor", "administrador" };
        if (!rolesValidos.Contains(dto.Rol))
            return (null, "Rol inválido. Valores válidos: paciente, doctor, administrador.", false);

        // Cualquiera se registra como paciente. Los demás roles los da de alta
        // un administrador, salvo el primero: sin ningún administrador activo
        // no habría quién lo cree.
        if (dto.Rol != "paciente" && !esAdministrador)
        {
            var esPrimerAdministrador = dto.Rol == "administrador" && !await _repo.HayAdministradorActivo();
            if (!esPrimerAdministrador)
                return (null, "No tenés permiso para crear una cuenta con ese rol.", true);
        }

        var existente = await _repo.ObtenerPorEmail(dto.Email);
        if (existente != null)
            return (null, "El email ya se encuentra registrado en el sistema.", false);

        // La ficha del paciente solo se acepta en el alta que hace un
        // administrador. En el registro público se ignora: si no, cualquiera
        // podría probar DNI y averiguar cuáles ya están cargados.
        PacienteUpdateDto? ficha = null;
        if (dto.Rol == "paciente" && esAdministrador)
            ficha = dto.Ficha;

        string? dni = null;
        if (ficha != null && !string.IsNullOrWhiteSpace(ficha.Dni))
            dni = ficha.Dni.Trim();

        // El DNI se revisa antes de crear nada, así un DNI repetido no deja
        // una cuenta creada a medias.
        if (dni != null && await _pacienteRepo.ExisteDniEnOtroPaciente(dni, 0))
            return (null, "Ya existe un paciente registrado con ese DNI.", false);

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

        // El perfil de paciente se arma junto con la cuenta: al guardar,
        // Entity Framework inserta las dos filas en la misma operación (o
        // ninguna, si algo falla).
        if (usuario.Rol == "paciente")
        {
            var paciente = new Paciente { Dni = dni };
            if (ficha != null)
                FichaPaciente.CopiarDatos(ficha, paciente);

            usuario.Paciente = paciente;
        }

        try
        {
            await _repo.Agregar(usuario);
        }
        catch (DatoRepetidoException)
        {
            // Otra alta con el mismo email o DNI se guardó un instante antes.
            return (null, "Ya existe una cuenta con ese email o un paciente con ese DNI.", false);
        }

        var token = GenerarToken(usuario);

        return (new RegistroResponseDto(usuario.Id, usuario.Email, usuario.Rol, token, usuario.Paciente?.Id), null, false);
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

        await _repo.Actualizar(usuario);
        return (true, null);
    }

    /// <summary>
    /// Cambia la contraseña de un usuario pidiendo la actual. La nueva tiene
    /// que tener al menos 8 caracteres y ser distinta de la actual.
    /// </summary>
    public async Task<(bool ok, string? error)> CambiarContrasena(int idUsuario, CambioContrasenaDto dto)
    {
        var usuario = await _repo.ObtenerPorId(idUsuario);
        if (usuario == null || !usuario.Activo)
            return (false, "Usuario no encontrado.");

        if (string.IsNullOrEmpty(dto.ContrasenaNueva) || dto.ContrasenaNueva.Length < 8)
            return (false, "La contraseña nueva debe tener al menos 8 caracteres.");

        // Verify compara la contraseña escrita con el hash guardado.
        if (string.IsNullOrEmpty(dto.ContrasenaActual) ||
            !BCrypt.Net.BCrypt.Verify(dto.ContrasenaActual, usuario.Contrasena))
            return (false, "La contraseña actual no es correcta.");

        if (dto.ContrasenaNueva == dto.ContrasenaActual)
            return (false, "La contraseña nueva tiene que ser distinta de la actual.");

        usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(dto.ContrasenaNueva);
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

    public async Task<(int total, IEnumerable<UsuarioListaDto> usuarios, string? error)> Buscar(
        string? buscar, string? rol, int pagina, int limite)
    {
        var rolesValidos = new[] { "paciente", "doctor", "administrador" };
        if (!string.IsNullOrEmpty(rol) && !rolesValidos.Contains(rol))
            return (0, Enumerable.Empty<UsuarioListaDto>(),
                "Rol inválido. Valores válidos: paciente, doctor, administrador.");

        var (total, usuarios) = await _repo.Buscar(buscar?.Trim(), rol, pagina, limite);
        var conFoto = (await _repo.ObtenerIdsConFoto(usuarios.Select(u => u.Id))).ToHashSet();

        var resultado = usuarios.Select(u => new UsuarioListaDto(
            u.Id,
            u.Nombre,
            u.Apellido,
            u.Email,
            u.Telefono,
            u.Rol,
            u.Activo,
            u.Paciente?.Id,
            u.Doctor?.Id,
            conFoto.Contains(u.Id)
        ));

        return (total, resultado, null);
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
