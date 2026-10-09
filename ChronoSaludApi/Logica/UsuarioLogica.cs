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
    private readonly ITurnoRepository _turnoRepo;
    private readonly IReloj _reloj;
    private readonly IConfiguration _config;

    public UsuarioLogica(
        IUsuarioRepository repo,
        IPacienteRepository pacienteRepo,
        ITurnoRepository turnoRepo,
        IReloj reloj,
        IConfiguration config)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _turnoRepo = turnoRepo;
        _reloj = reloj;
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

        return new UsuarioDto(u.Id, u.Nombre, u.Apellido, u.Email, u.Telefono, u.Rol, u.Activo);
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

    /// <summary>
    /// Baja lógica: la cuenta deja de poder entrar y, si es de un doctor, el
    /// doctor deja de aparecer y de recibir turnos. No se borra nada: la
    /// historia clínica, las recetas y los turnos pasados quedan.
    /// Se frena (con el motivo) si es la propia cuenta, si es el último
    /// administrador o si tiene turnos en pie desde hoy.
    /// </summary>
    public async Task<(bool ok, string? error)> DarDeBaja(int id, int idSolicitante)
    {
        var usuario = await _repo.ObtenerConDoctor(id);
        if (usuario == null)
            return (false, "Usuario no encontrado.");

        if (!usuario.Activo)
            return (false, "La cuenta ya está dada de baja.");

        if (usuario.Id == idSolicitante)
            return (false, "No podés dar de baja tu propia cuenta.");

        if (usuario.Rol == "administrador" && await _repo.ContarAdministradoresActivos() <= 1)
            return (false, "Es el único administrador activo: sin él nadie podría administrar el sistema.");

        var turnosEnPie = await ContarTurnosEnPie(usuario);
        if (turnosEnPie > 0)
        {
            var cuantos = turnosEnPie == 1
                ? "1 turno pendiente o confirmado"
                : $"{turnosEnPie} turnos pendientes o confirmados";
            return (false, $"La cuenta tiene {cuantos} desde hoy. Cancelalos o reprogramalos antes de darla de baja.");
        }

        usuario.Activo = false;
        if (usuario.Doctor != null)
            usuario.Doctor.Activo = false;

        await _repo.GuardarActivo(usuario);
        return (true, null);
    }

    /// <summary>
    /// Vuelve a activar una cuenta dada de baja y, si tiene, su perfil de
    /// doctor. El horario del doctor quedó guardado, así que vuelve igual.
    /// </summary>
    public async Task<(bool ok, string? error)> Reactivar(int id)
    {
        var usuario = await _repo.ObtenerConDoctor(id);
        if (usuario == null)
            return (false, "Usuario no encontrado.");

        if (usuario.Activo)
            return (false, "La cuenta ya está activa.");

        usuario.Activo = true;
        if (usuario.Doctor != null)
            usuario.Doctor.Activo = true;

        await _repo.GuardarActivo(usuario);
        return (true, null);
    }

    /// <summary>
    /// Cuántos turnos pendientes o confirmados tiene la cuenta desde hoy (hora
    /// de Argentina), como paciente o como doctor.
    /// </summary>
    private async Task<int> ContarTurnosEnPie(Usuario usuario)
    {
        var total = 0;

        var paciente = await _pacienteRepo.ObtenerPorIdUsuario(usuario.Id);
        if (paciente != null)
            total += await ContarTurnosEnPie(pacienteId: paciente.Id, doctorId: null);

        if (usuario.Doctor != null)
            total += await ContarTurnosEnPie(pacienteId: null, doctorId: usuario.Doctor.Id);

        return total;
    }

    private async Task<int> ContarTurnosEnPie(int? pacienteId, int? doctorId)
    {
        var filtro = new FiltroTurnos
        {
            PacienteId = pacienteId,
            DoctorId = doctorId,
            Desde = _reloj.Ahora().Date
        };
        filtro.Estados.Add(EstadosTurno.Pendiente);
        filtro.Estados.Add(EstadosTurno.Confirmado);

        // Alcanza con el total: se pide una página de un solo turno.
        var (total, _) = await _turnoRepo.Buscar(filtro, "fecha", false, 1, 1);
        return total;
    }

    public async Task<(int total, IEnumerable<UsuarioListaDto> usuarios, string? error)> Buscar(
        string? buscar, string? rol, int pagina, int limite, bool bajas = false)
    {
        var rolesValidos = new[] { "paciente", "doctor", "administrador" };
        if (!string.IsNullOrEmpty(rol) && !rolesValidos.Contains(rol))
            return (0, Enumerable.Empty<UsuarioListaDto>(),
                "Rol inválido. Valores válidos: paciente, doctor, administrador.");

        var (total, usuarios) = await _repo.Buscar(buscar?.Trim(), rol, pagina, limite, bajas);
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
