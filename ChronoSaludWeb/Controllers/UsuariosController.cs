using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class UsuariosController : ControladorBase
{
    private const string TituloSinPermiso = "Esta sección es solo para administradores";
    private const string MotivoSinPermiso =
        "Buscar usuarios y editar el perfil de otra persona está reservado al rol administrador.";

    // La API contesta el 403 con el cuerpo vacío, así que el mensaje se arma acá.
    private const string SinPermisoDeLaApi = "No tenés permiso para modificar este usuario";

    // Clave de ModelState de los errores generales de cada tarjeta: es el mismo
    // prefijo con el que se enlaza su sub-modelo.
    private const string PrefijoCuenta = nameof(UsuarioEditarViewModel.Cuenta);
    private const string PrefijoPaciente = nameof(UsuarioEditarViewModel.Paciente);
    private const string PrefijoDoctor = nameof(UsuarioEditarViewModel.Doctor);

    private readonly UsuarioService _usuarios;
    private readonly PacienteService _pacientes;
    private readonly DoctorService _doctores;
    private readonly AuthService _auth;

    public UsuariosController(
        UsuarioService usuarios, PacienteService pacientes, DoctorService doctores, AuthService auth)
    {
        _usuarios = usuarios;
        _pacientes = pacientes;
        _doctores = doctores;
        _auth = auth;
    }

    public async Task<IActionResult> Index(string? buscar, string? rol, int pagina = 1)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        // Lo que llega por la URL se lleva a valores válidos en vez de
        // contestar con un error: un rol desconocido es "todos" y una página
        // menor a 1 es la primera.
        var texto = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();
        var filtroRol = UsuariosIndexViewModel.RolesFiltrables.Contains(rol) ? rol : null;
        pagina = Math.Max(pagina, 1);

        try
        {
            var resultado = await _usuarios.BuscarAsync(texto, filtroRol, pagina, UsuariosIndexViewModel.PorPagina);

            // Una página más allá de la última (por la URL, o porque dieron de
            // baja usuarios mientras se navegaba) vuelve a la última que existe.
            if (resultado.Usuarios.Count == 0 && resultado.Total > 0)
            {
                var ultima = (int)Math.Ceiling(resultado.Total / (double)UsuariosIndexViewModel.PorPagina);
                return RedirectToAction(nameof(Index), new { buscar = texto, rol = filtroRol, pagina = ultima });
            }

            return View(new UsuariosIndexViewModel
            {
                Buscar = texto,
                Rol = filtroRol,
                Pagina = pagina,
                Total = resultado.Total,
                Usuarios = resultado.Usuarios
                    .Select(u => new UsuarioFilaViewModel
                    {
                        IdUsuario = u.IdUsuario,
                        Nombre = u.Nombre,
                        Apellido = u.Apellido,
                        Email = u.Email,
                        Rol = u.Rol
                    })
                    .ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new UsuariosIndexViewModel { Buscar = texto, Rol = filtroRol, Error = MensajeDe(error) });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, int? idPaciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar), new { id, idPaciente }));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var (modelo, salida) = await CargarAsync(id, idPaciente);
        return salida ?? View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarCuenta(
        int id, int? idPaciente, [Bind(Prefix = PrefijoCuenta)] CuentaEditarViewModel cuenta)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar), new { id, idPaciente }));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var (modelo, salida) = await CargarAsync(id, idPaciente);
        if (salida is not null) return salida;

        modelo!.Cuenta = cuenta;

        if (!ModelState.IsValid)
            return View(nameof(Editar), modelo);

        var nombre = cuenta.Nombre.Trim();

        try
        {
            await _usuarios.ActualizarAsync(id, nombre, cuenta.Apellido.Trim(), cuenta.Telefono?.Trim());
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(PrefijoCuenta, MensajeDe(error));
            return View(nameof(Editar), modelo);
        }

        // Si el administrador se editó a sí mismo, el nombre del encabezado
        // sale de la sesión: se lo actualiza para no esperar al próximo login.
        if (_auth.SesionActual is { } sesion && sesion.IdUsuario == id)
            HttpContext.Session.GuardarSesion(sesion with { Nombre = nombre });

        TempData["Exito"] = "Datos de la cuenta actualizados.";
        return RedirectToAction(nameof(Editar), new { id, idPaciente });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPaciente(
        int id, int idPaciente, [Bind(Prefix = PrefijoPaciente)] PacienteEditarViewModel paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar), new { id, idPaciente }));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        // Vuelve a verificar que ese paciente sea de ese usuario: los dos ids
        // viajan en la URL del formulario.
        var (modelo, salida) = await CargarAsync(id, idPaciente);
        if (salida is not null) return salida;

        modelo!.Paciente = paciente;

        if (!ModelState.IsValid)
            return View(nameof(Editar), modelo);

        try
        {
            await _pacientes.ActualizarFichaAsync(
                idPaciente,
                paciente.FechaNacimiento?.ToDateTime(TimeOnly.MinValue),
                paciente.Sexo,
                paciente.GrupoSanguineo,
                paciente.Alergias?.Trim(),
                paciente.Condiciones?.Trim(),
                paciente.Dni?.Trim(),
                paciente.Direccion?.Trim(),
                paciente.Nacionalidad?.Trim(),
                paciente.EstadoCivil);
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status409Conflict)
        {
            // El único 409 de PUT /pacientes/{id} es el DNI repetido.
            ModelState.AddModelError($"{PrefijoPaciente}.{nameof(PacienteEditarViewModel.Dni)}", error.Message);
            return View(nameof(Editar), modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(PrefijoPaciente, MensajeDe(error));
            return View(nameof(Editar), modelo);
        }

        TempData["Exito"] = "Ficha del paciente actualizada.";
        return RedirectToAction(nameof(Editar), new { id, idPaciente });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarDoctor(
        int id, int? idPaciente, [Bind(Prefix = PrefijoDoctor)] DoctorEditarViewModel doctor)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar), new { id, idPaciente }));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var (modelo, salida) = await CargarAsync(id, idPaciente);
        if (salida is not null) return salida;

        // El IdDoctor no viaja en el formulario: lo resuelve CargarAsync contra
        // la API a partir del usuario, así no se puede apuntar al doctor de otro.
        if (modelo!.IdDoctor is not { } idDoctor)
            return NoEncontrado(id, "Doctor no encontrado", "perfil de doctor para el usuario");

        modelo.Doctor = doctor;

        if (!ModelState.IsValid)
            return View(nameof(Editar), modelo);

        try
        {
            await _doctores.ActualizarAsync(idDoctor, doctor.Especialidad.Trim(), doctor.Consultorio?.Trim());
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(PrefijoDoctor, MensajeDe(error));
            return View(nameof(Editar), modelo);
        }

        TempData["Exito"] = "Perfil de doctor actualizado.";
        return RedirectToAction(nameof(Editar), new { id, idPaciente });
    }

    /// <summary>
    /// Arma la pantalla con lo que hay en la API. Devuelve una salida (404) en
    /// vez del modelo si el usuario no existe o si el paciente pedido no existe
    /// o es de otro usuario.
    /// </summary>
    private async Task<(UsuarioEditarViewModel? modelo, IActionResult? salida)> CargarAsync(int id, int? idPaciente)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id);
        if (usuario is null)
            return (null, NoEncontrado(id, "Usuario no encontrado", "usuario"));

        // De la fila del buscador salen los ids de perfil que UsuarioDto no
        // trae. Si se entró desde la ficha del paciente, idPaciente ya viene en
        // la URL y se verifica abajo; si no, se usa el que informa la API.
        var fila = await _usuarios.ObtenerFilaAsync(usuario);
        idPaciente ??= fila?.IdPaciente;

        // El perfil de doctor va aparte: si no se pudo traer, la pantalla se
        // muestra igual sin esa tarjeta.
        var idDoctor = fila?.IdDoctor;
        var doctor = idDoctor is { } idPerfil ? await _doctores.ObtenerPorIdAsync(idPerfil) : null;

        PacienteDetalle? paciente = null;
        if (idPaciente is { } idFicha)
        {
            paciente = await _pacientes.ObtenerPorIdAsync(idFicha);
            if (paciente is null || paciente.IdUsuario != id)
                return (null, NoEncontrado(idFicha, "Paciente no encontrado", "paciente de ese usuario"));
        }

        var modelo = new UsuarioEditarViewModel
        {
            IdUsuario = usuario.IdUsuario,
            IdPaciente = paciente?.IdPaciente,
            NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim(),
            Email = usuario.Email,
            Rol = usuario.Rol,
            Cuenta = new CuentaEditarViewModel
            {
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono
            },
            Paciente = paciente is null
                ? null
                : new PacienteEditarViewModel
                {
                    Dni = paciente.Dni,
                    FechaNacimiento = paciente.FechaNacimiento is { } nacimiento
                        ? DateOnly.FromDateTime(nacimiento)
                        : null,
                    Sexo = paciente.Sexo,
                    GrupoSanguineo = paciente.GrupoSanguineo,
                    Nacionalidad = paciente.Nacionalidad,
                    EstadoCivil = paciente.EstadoCivil,
                    Direccion = paciente.Direccion,
                    Alergias = paciente.Alergias,
                    Condiciones = paciente.Condiciones
                },
            IdDoctor = doctor?.IdDoctor,
            Matricula = doctor?.Matricula,
            Doctor = doctor is null
                ? null
                : new DoctorEditarViewModel
                {
                    Especialidad = doctor.Especialidad,
                    Consultorio = doctor.Consultorio
                }
        };

        return (modelo, null);
    }

    private static string MensajeDe(ApiException error) =>
        error.Status == StatusCodes.Status403Forbidden ? SinPermisoDeLaApi : error.Message;
}
