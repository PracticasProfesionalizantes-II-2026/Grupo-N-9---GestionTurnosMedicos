using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class UsuariosController : ControladorBase
{
    private const string TituloSinPermiso = "Esta sección es solo para administradores";
    private const string MotivoSinPermiso =
        "Editar el perfil de otra persona está reservado al rol administrador.";

    // La API contesta el 403 con el cuerpo vacío, así que el mensaje se arma acá.
    private const string SinPermisoDeLaApi = "No tenés permiso para modificar este usuario";

    // Clave de ModelState de los errores generales de cada tarjeta: es el mismo
    // prefijo con el que se enlaza su sub-modelo.
    private const string PrefijoCuenta = nameof(UsuarioEditarViewModel.Cuenta);
    private const string PrefijoPaciente = nameof(UsuarioEditarViewModel.Paciente);

    private readonly UsuarioService _usuarios;
    private readonly PacienteService _pacientes;
    private readonly AuthService _auth;

    public UsuariosController(UsuarioService usuarios, PacienteService pacientes, AuthService auth)
    {
        _usuarios = usuarios;
        _pacientes = pacientes;
        _auth = auth;
    }

    // Todavía no hay listado de usuarios (la API no tiene con qué armarlo).
    // La acción existe para que los "volver" de SinPermiso y NoEncontrado, que
    // apuntan al Index del controlador, tengan a dónde ir.
    public IActionResult Index()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        return RedirectToAction("Index", "Admin");
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
                }
        };

        return (modelo, null);
    }

    private static string MensajeDe(ApiException error) =>
        error.Status == StatusCodes.Status403Forbidden ? SinPermisoDeLaApi : error.Message;
}
