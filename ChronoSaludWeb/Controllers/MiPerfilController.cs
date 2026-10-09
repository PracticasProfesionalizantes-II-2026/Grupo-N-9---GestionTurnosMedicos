using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

/// <summary>
/// Mi perfil: cada uno ve y corrige sus propios datos y cambia su contraseña.
/// Los ids salen siempre de la sesión o de /pacientes/me y /doctores/me, nunca
/// de la URL ni del formulario: así nadie puede apuntar a la cuenta de otro.
/// </summary>
public class MiPerfilController : ControladorBase
{
    private const string TituloSinPermiso = "La ficha es solo para pacientes";
    private const string MotivoSinPermiso =
        "La ficha clínica es propia de cada paciente, así que solo se puede " +
        "completar con una cuenta de paciente.";

    private const string SinFicha =
        "Tu usuario todavía no tiene un perfil de paciente asociado. Pedile a la administración que lo cree.";

    // Clave de ModelState de los errores generales de cada tarjeta: es el mismo
    // prefijo con el que se enlaza su sub-modelo (igual que en Usuarios/Editar).
    private const string PrefijoCuenta = nameof(MiPerfilEditarViewModel.Cuenta);
    private const string PrefijoPaciente = nameof(MiPerfilEditarViewModel.Paciente);

    private readonly UsuarioService _usuarios;
    private readonly PacienteService _pacientes;
    private readonly DoctorService _doctores;
    private readonly AuthService _auth;

    public MiPerfilController(
        UsuarioService usuarios, PacienteService pacientes, DoctorService doctores, AuthService auth)
    {
        _usuarios = usuarios;
        _pacientes = pacientes;
        _doctores = doctores;
        _auth = auth;
    }

    /// <summary>Los datos de la cuenta y, según el rol, la ficha o los datos profesionales.</summary>
    public async Task<IActionResult> Index()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        var sesion = _auth.SesionActual!;

        var usuario = await _usuarios.ObtenerPorIdAsync(sesion.IdUsuario);
        if (usuario is null)
            return SinCuenta();

        PacienteDetalle? paciente = null;
        if (usuario.Rol == "paciente")
            paciente = await _pacientes.ObtenerMiPerfilAsync();

        DoctorDetalle? doctor = null;
        if (usuario.Rol == "doctor")
            doctor = await _doctores.ObtenerMiPerfilAsync();

        return View(new MiPerfilViewModel
        {
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            Rol = usuario.Rol,
            Ficha = paciente is null ? null : ArmarFicha(paciente),
            FaltaFicha = usuario.Rol == "paciente" && paciente is null,
            IdDoctor = doctor?.IdDoctor,
            Especialidad = doctor?.Especialidad,
            Matricula = doctor?.Matricula,
            Consultorio = doctor?.Consultorio
        });
    }

    [HttpGet]
    public async Task<IActionResult> Editar()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar)));

        var (modelo, salida) = await CargarEdicionAsync();
        return salida ?? View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarCuenta([Bind(Prefix = PrefijoCuenta)] CuentaEditarViewModel cuenta)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar)));

        var (modelo, salida) = await CargarEdicionAsync();
        if (salida is not null) return salida;

        modelo!.Cuenta = cuenta;

        if (!ModelState.IsValid)
            return View(nameof(Editar), modelo);

        var sesion = _auth.SesionActual!;
        var nombre = cuenta.Nombre.Trim();

        try
        {
            await _usuarios.ActualizarAsync(sesion.IdUsuario, nombre, cuenta.Apellido.Trim(), cuenta.Telefono?.Trim());
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(PrefijoCuenta, error.Message);
            return View(nameof(Editar), modelo);
        }

        // El nombre del encabezado sale de la sesión: se lo actualiza para no
        // esperar al próximo login.
        HttpContext.Session.GuardarSesion(sesion with { Nombre = nombre });

        TempData["Exito"] = "Tus datos se guardaron.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarFicha([Bind(Prefix = PrefijoPaciente)] PacienteEditarViewModel paciente)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Editar)));

        if (_auth.SesionActual!.Rol != "paciente")
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso, volverAlInicio: true);

        var (modelo, salida) = await CargarEdicionAsync();
        if (salida is not null) return salida;

        if (modelo!.IdPaciente is not { } idPaciente)
        {
            TempData["Error"] = SinFicha;
            return RedirectToAction(nameof(Index));
        }

        // Un DNI ya guardado no se manda: el paciente no lo puede cambiar.
        if (modelo.DniGuardado is not null)
            paciente.Dni = null;

        modelo.Paciente = paciente;

        if (!ModelState.IsValid)
            return View(nameof(Editar), modelo);

        try
        {
            // esPersonal: false, así un DNI vacío no se pide borrar (la API
            // solo se lo permite a la administración).
            await _pacientes.ActualizarFichaAsync(idPaciente, paciente.ArmarFicha(esPersonal: false));
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status409Conflict)
        {
            // El único 409 de PUT /pacientes/{id} es el DNI repetido.
            ModelState.AddModelError($"{PrefijoPaciente}.{nameof(PacienteEditarViewModel.Dni)}", error.Message);
            return View(nameof(Editar), modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(PrefijoPaciente, error.Message);
            return View(nameof(Editar), modelo);
        }

        TempData["Exito"] = "Tu ficha se guardó.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Contrasena()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Contrasena)));

        return View(new CambioContrasenaViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contrasena(CambioContrasenaViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Contrasena)));

        // Se revisa acá también, así no gasta uno de los intentos de la API.
        if (!string.IsNullOrEmpty(modelo.Nueva) && modelo.Nueva == modelo.Actual)
            ModelState.AddModelError(nameof(modelo.Nueva), "La contraseña nueva tiene que ser distinta de la actual.");

        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await _usuarios.CambiarContrasenaAsync(modelo.Actual, modelo.Nueva);
        }
        catch (ApiException error) when (error.Status == StatusCodes.Status400BadRequest)
        {
            ModelState.AddModelError(CampoDelError(error.Message), error.Message);
            return View(modelo);
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // Demasiados intentos (429), la API caída, etc.: va arriba del formulario.
            ModelState.AddModelError(string.Empty, error.Message);
            return View(modelo);
        }

        TempData["Exito"] = "Tu contraseña se cambió. La próxima vez entrá con la nueva.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// La pantalla vieja de "completar la ficha". Queda para que los enlaces
    /// que ya existan sigan andando: ahora la ficha se completa en Editar.
    /// </summary>
    [HttpGet]
    public IActionResult CompletarPaciente() => RedirectToAction(nameof(Editar));

    /// <summary>
    /// Arma Mi perfil/Editar con lo que hay en la API. Devuelve una salida en
    /// vez del modelo si no se pudo leer la cuenta.
    /// </summary>
    private async Task<(MiPerfilEditarViewModel? modelo, IActionResult? salida)> CargarEdicionAsync()
    {
        var sesion = _auth.SesionActual!;

        var usuario = await _usuarios.ObtenerPorIdAsync(sesion.IdUsuario);
        if (usuario is null)
            return (null, SinCuenta());

        PacienteDetalle? paciente = null;
        if (usuario.Rol == "paciente")
            paciente = await _pacientes.ObtenerMiPerfilAsync();

        string? dniGuardado = null;
        if (paciente is not null && !string.IsNullOrWhiteSpace(paciente.Dni))
            dniGuardado = paciente.Dni;

        var modelo = new MiPerfilEditarViewModel
        {
            Email = usuario.Email,
            Rol = usuario.Rol,
            Cuenta = new CuentaEditarViewModel
            {
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono
            },
            IdPaciente = paciente?.IdPaciente,
            Paciente = paciente is null ? null : PacienteEditarViewModel.Desde(paciente),
            DniGuardado = dniGuardado
        };

        return (modelo, null);
    }

    /// <summary>La ficha del paciente para la tarjeta "Tu ficha".</summary>
    private static PacienteDetalleViewModel ArmarFicha(PacienteDetalle paciente) => new()
    {
        IdPaciente = paciente.IdPaciente,
        IdUsuario = paciente.IdUsuario,
        Nombre = paciente.Nombre,
        Apellido = paciente.Apellido,
        FechaNacimiento = paciente.FechaNacimiento,
        Sexo = paciente.Sexo,
        GrupoSanguineo = paciente.GrupoSanguineo,
        Alergias = paciente.Alergias,
        Condiciones = paciente.Condiciones,
        TipoDocumento = paciente.TipoDocumento,
        Dni = paciente.Dni,
        Nacionalidad = paciente.Nacionalidad,
        EstadoCivil = paciente.EstadoCivil,
        Telefono = paciente.Telefono,
        Direccion = paciente.Direccion,
        Localidad = paciente.Localidad,
        Provincia = paciente.Provincia,
        CodigoPostal = paciente.CodigoPostal,
        ContactoEmergenciaNombre = paciente.ContactoEmergenciaNombre,
        ContactoEmergenciaTelefono = paciente.ContactoEmergenciaTelefono
    };

    /// <summary>
    /// A qué campo va un 400 de la API: si falló la contraseña actual, debajo
    /// de "Contraseña actual"; si no (largo o repetida), debajo de la nueva.
    /// </summary>
    public static string CampoDelError(string mensaje)
    {
        if (mensaje.Contains("actual no es correcta"))
            return nameof(CambioContrasenaViewModel.Actual);

        return nameof(CambioContrasenaViewModel.Nueva);
    }

    /// <summary>
    /// La cuenta de la sesión no aparece en la API (por ejemplo, la dieron de
    /// baja mientras estaba logueado): se vuelve al inicio con el aviso.
    /// </summary>
    private IActionResult SinCuenta()
    {
        TempData["Error"] = "No pudimos leer los datos de tu cuenta. Probá de nuevo en un momento.";
        return RedirectToAction("Index", "Home");
    }
}
