using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Models.ViewModels;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class PacientesController : ControladorBase
{
    private const int Limite = 100;

    private const string TituloSinPermiso = "No podés ver los pacientes con tu rol";
    private const string MotivoSinPermiso =
        "El listado de pacientes es solo para doctores y administradores, " +
        "porque la ficha incluye datos clínicos.";

    private const string TituloSinPermisoAlta = "No podés dar de alta pacientes con tu rol";
    private const string MotivoSinPermisoAlta =
        "El alta de pacientes está reservada al rol administrador.";

    private readonly PacienteService _pacientes;
    private readonly CoberturaService _coberturas;
    private readonly TurnoService _turnos;
    private readonly UsuarioService _usuarios;
    private readonly AuthService _auth;

    public PacientesController(
        PacienteService pacientes,
        CoberturaService coberturas,
        TurnoService turnos,
        UsuarioService usuarios,
        AuthService auth)
    {
        _pacientes = pacientes;
        _coberturas = coberturas;
        _turnos = turnos;
        _usuarios = usuarios;
        _auth = auth;
    }

    public async Task<IActionResult> Index(string? nombre)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedeVerPacientes)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var busqueda = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();

        try
        {
            var pagina = await _pacientes.BuscarAsync(busqueda, Limite);

            // Un solo pedido para toda la página: quiénes tienen foto.
            var fotos = await _usuarios.ObtenerFotosAsync(pacientes: pagina.Pacientes.Select(p => p.IdPaciente));

            return View(new PacientesIndexViewModel
            {
                Nombre = busqueda,
                Total = pagina.Total,
                Pacientes = pagina.Pacientes
                    .Select(p => new PacienteFilaViewModel
                    {
                        IdPaciente = p.IdPaciente,
                        Nombre = p.Nombre,
                        Apellido = p.Apellido,
                        FotoUrl = UrlDeFoto(fotos, p.IdPaciente)
                    })
                    .ToList()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new PacientesIndexViewModel { Nombre = busqueda, Error = error.Message });
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        // Mismo criterio que el listado: acá se ven alergias y condiciones.
        if (!_auth.PuedeVerPacientes)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        try
        {
            var paciente = await _pacientes.ObtenerPorIdAsync(id);

            if (paciente is null)
                return NoEncontrado(id, "Paciente no encontrado", "paciente");

            // Coberturas y turnos son datos "extra" de la ficha: si cualquiera de
            // los dos falla, se muestra igual la ficha con lo que sí llegó, en
            // vez de tirar toda la página abajo por un servicio secundario.
            var coberturas = await ObtenerCoberturasSinRomperAsync(id);
            var ultimoTurno = await ObtenerUltimoTurnoSinRomperAsync(id);
            var fotos = await _usuarios.ObtenerFotosAsync(pacientes: new[] { paciente.IdPaciente });

            return View(new PacienteDetalleViewModel
            {
                IdPaciente = paciente.IdPaciente,
                IdUsuario = paciente.IdUsuario,
                FotoUrl = UrlDeFoto(fotos, paciente.IdPaciente),
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
                ContactoEmergenciaTelefono = paciente.ContactoEmergenciaTelefono,
                Coberturas = coberturas,
                UltimoTurno = ultimoTurno
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new PacienteDetalleViewModel { IdPaciente = id, Error = error.Message });
        }
    }

    [HttpGet]
    public IActionResult Crear()
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermisoAlta, MotivoSinPermisoAlta);

        return View(new PacienteCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PacienteCreateViewModel modelo)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Crear)));

        if (!_auth.EsAdministrador)
            return SinPermiso(TituloSinPermisoAlta, MotivoSinPermisoAlta);

        // La foto es opcional, pero si vino se valida antes de crear nada: un
        // archivo que no sirve frena el alta en vez de dejarla sin foto.
        ImagenValidada? foto = null;
        if (modelo.Foto is { Length: > 0 })
        {
            (foto, var errorFoto) = await ValidadorDeImagen.ValidarAsync(modelo.Foto);
            if (foto is null)
                ModelState.AddModelError(nameof(modelo.Foto), errorFoto!);
        }

        if (!ModelState.IsValid)
            return View(modelo);

        RegistroRespuesta registro;
        try
        {
            // La cuenta y la ficha viajan juntas: la API crea el usuario y el
            // paciente en una sola operación, o ninguno de los dos.
            registro = await _auth.RegistrarComoPacienteAsync(
                modelo.Nombre, modelo.Apellido, modelo.Email, modelo.Contrasena, modelo.Telefono,
                modelo.ArmarFicha());
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // Un DNI repetido se muestra junto a su campo; el resto, arriba.
            if (error.Message.Contains("paciente registrado con ese DNI"))
                ModelState.AddModelError(nameof(modelo.NumeroDocumento), error.Message);
            else
                ModelState.AddModelError(string.Empty, error.Message);

            return View(modelo);
        }

        TempData["Exito"] = $"Paciente \"{modelo.Nombre} {modelo.Apellido}\" dado de alta correctamente.";

        if (foto is not null)
        {
            try
            {
                await _usuarios.SubirFotoAsync(registro.IdUsuario, foto);
            }
            catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
            {
                // El paciente ya quedó creado (no hay transacción entre los dos
                // pedidos): se avisa y la foto se puede cargar después desde
                // Usuarios/Editar.
                TempData["Error"] =
                    $"El paciente quedó dado de alta, pero no se pudo guardar la foto: {error.Message}. " +
                    "Podés subirla desde \"Editar perfil\" en su ficha.";
            }
        }

        // Al terminar se muestra la ficha del paciente nuevo, para revisar lo cargado.
        if (registro.IdPaciente != null)
            return RedirectToAction(nameof(Detalle), new { id = registro.IdPaciente.Value });

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// /Usuarios/Foto/{idUsuario} si la API informó que ese paciente tiene una
    /// foto visible; null si no, así el avatar no pide una imagen que no existe.
    /// </summary>
    private string? UrlDeFoto(FotosDisponibles fotos, int idPaciente) =>
        fotos.Pacientes.TryGetValue(idPaciente, out var idUsuario)
            ? Url.Action("Foto", "Usuarios", new { id = idUsuario })
            : null;

    private async Task<IReadOnlyList<CoberturaFilaViewModel>> ObtenerCoberturasSinRomperAsync(int idPaciente)
    {
        try
        {
            var coberturas = await _coberturas.ObtenerDePacienteAsync(idPaciente);
            return coberturas
                .Select(c => new CoberturaFilaViewModel
                {
                    NombreCobertura = c.NombreCobertura,
                    Plan = c.Plan,
                    IdAfiliado = c.IdAfiliado
                })
                .ToList();
        }
        catch (ApiException)
        {
            return Array.Empty<CoberturaFilaViewModel>();
        }
    }

    /// <summary>
    /// El día del último turno atendido (completado). Se le pide a la API uno
    /// solo, ordenado del más nuevo al más viejo. Un turno futuro o cancelado
    /// no cuenta como "último turno".
    /// </summary>
    private async Task<DateTime?> ObtenerUltimoTurnoSinRomperAsync(int idPaciente)
    {
        try
        {
            var pagina = await _turnos.ObtenerAsync(
                pacienteId: idPaciente,
                estado: "completado",
                orden: "fecha",
                descendente: true,
                limite: 1);

            if (pagina.Turnos.Count == 0)
                return null;

            return pagina.Turnos[0].FechaInicio;
        }
        catch (ApiException)
        {
            return null;
        }
    }
}
