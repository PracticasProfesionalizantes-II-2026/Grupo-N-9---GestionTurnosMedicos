using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class HomeController : Controller
{
    // Cuántos turnos entran en el panel de contexto. La API pagina de a 20.
    private const int TurnosDelPanel = 6;

    private readonly AuthService _auth;
    private readonly TurnoService _turnos;
    private readonly PacienteService _pacientes;
    private readonly DoctorService _doctores;

    public HomeController(
        AuthService auth,
        TurnoService turnos,
        PacienteService pacientes,
        DoctorService doctores)
    {
        _auth = auth;
        _turnos = turnos;
        _pacientes = pacientes;
        _doctores = doctores;
    }

    public async Task<IActionResult> Index()
    {
        var sesion = _auth.SesionActual;

        // Sin sesión: portada.
        if (sesion is null)
            return View(new DashboardViewModel());

        try
        {
            // La API solo emite estos tres roles (ver UsuarioLogica.Registrar).
            return sesion.Rol switch
            {
                "paciente"      => View(await ArmarPacienteAsync(sesion)),
                "doctor"        => View(await ArmarDoctorAsync(sesion)),
                "administrador" => View(await ArmarAdministradorAsync(sesion)),
                _               => View(ArmarRolDesconocido(sesion))
            };
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // El 401 lo maneja ApiExceptionFilter mandando al login.
            return View(new DashboardViewModel
            {
                Rol = sesion.Rol,
                Nombre = sesion.Nombre,
                Error = error.Message
            });
        }
    }

    /// <summary>
    /// Dos llamadas: el perfil de paciente (para conocer el IdPaciente, que no
    /// es el IdUsuario) y sus turnos de hoy en adelante.
    /// </summary>
    private async Task<DashboardViewModel> ArmarPacienteAsync(SesionUsuario sesion)
    {
        var perfil = await _pacientes.ObtenerMiPerfilAsync();

        var turnos = perfil is null
            ? Array.Empty<TurnoFilaViewModel>()
            : (await _turnos.ObtenerAsync(
                    pacienteId: perfil.IdPaciente,
                    desde: DateTime.Today,
                    limite: TurnosDelPanel))
                .Turnos.Select(TurnoFilaViewModel.Desde).ToArray();

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = sesion.Nombre,
            Aviso = perfil is null
                ? "Tu usuario todavía no tiene un perfil de paciente asociado, así que no podemos mostrar tus turnos. Pedile a la administración que lo cree."
                : null,
            Accesos = new[]
            {
                new AccesoRapidoViewModel
                {
                    Titulo = "Agendar turno",
                    Icono = "calendario-mas",
                    Motivo = "Para agendar hay que elegir el paciente de una lista, y la API solo se la muestra a doctor y administrador."
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Mis turnos",
                    Icono = "calendario",
                    Controlador = "Turnos",
                    Accion = "Index"
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Recetas",
                    Icono = "pastilla",
                    Motivo = "La sección de recetas todavía no está implementada."
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Mis coberturas",
                    Icono = "escudo",
                    Motivo = "La sección de coberturas todavía no está implementada."
                }
            },
            Panel = new PanelTurnosViewModel
            {
                Titulo = "Próximos turnos",
                Vista = VistaPanel.Paciente,
                Turnos = turnos,
                TextoVacio = "No tenés turnos programados de hoy en adelante."
            }
        };
    }

    /// <summary>
    /// Dos llamadas: el perfil de doctor (para el IdDoctor) y sus turnos de hoy.
    /// </summary>
    private async Task<DashboardViewModel> ArmarDoctorAsync(SesionUsuario sesion)
    {
        var perfil = await _doctores.ObtenerMiPerfilAsync();

        var turnos = perfil is null
            ? Array.Empty<TurnoFilaViewModel>()
            : (await _turnos.ObtenerAsync(
                    doctorId: perfil.IdDoctor,
                    desde: DateTime.Today,
                    hasta: DateTime.Today,
                    limite: TurnosDelPanel))
                .Turnos.Select(TurnoFilaViewModel.Desde).ToArray();

        var hoy = DateTime.Today.ToString("yyyy-MM-dd");

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = sesion.Nombre,
            Aviso = perfil is null
                ? "Tu usuario tiene rol doctor pero no tiene un perfil de doctor cargado (matrícula, especialidad), así que la API no puede decirnos qué turnos son tuyos. Un administrador lo crea desde POST /doctores."
                : null,
            Accesos = new[]
            {
                new AccesoRapidoViewModel
                {
                    Titulo = "Turnos del día",
                    Icono = "calendario",
                    Controlador = "Turnos",
                    Accion = "Index",
                    Ruta = new Dictionary<string, string> { ["desde"] = hoy, ["hasta"] = hoy }
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Historia clínica",
                    Icono = "historia",
                    Motivo = "La sección de historia clínica todavía no está implementada."
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Recetas",
                    Icono = "pastilla",
                    Motivo = "La sección de recetas todavía no está implementada."
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Coberturas",
                    Icono = "escudo",
                    Motivo = "La sección de coberturas todavía no está implementada."
                }
            },
            Panel = new PanelTurnosViewModel
            {
                Titulo = "Turnos de hoy",
                Vista = VistaPanel.Doctor,
                Turnos = turnos,
                TextoVacio = "No tenés turnos agendados para hoy."
            }
        };
    }

    /// <summary>
    /// Dos llamadas: los turnos de hoy (de ahí salen la métrica y el panel) y el
    /// conteo de pacientes, que se pide con limite=1 para quedarse solo con el
    /// "total" en vez de traer la tabla entera.
    /// </summary>
    private async Task<DashboardViewModel> ArmarAdministradorAsync(SesionUsuario sesion)
    {
        var turnosHoy = await _turnos.ObtenerAsync(
            desde: DateTime.Today,
            hasta: DateTime.Today,
            limite: TurnosDelPanel);

        var totalPacientes = await _pacientes.ContarAsync();

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = sesion.Nombre,
            TurnosDeHoy = turnosHoy.Total,
            TotalPacientes = totalPacientes,
            Accesos = new[]
            {
                new AccesoRapidoViewModel
                {
                    Titulo = "Nuevo turno",
                    Icono = "calendario-mas",
                    Controlador = "Turnos",
                    Accion = "Crear"
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Pacientes",
                    Icono = "personas",
                    Motivo = "El listado de pacientes todavía no está implementado en el front."
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Buscar",
                    Icono = "lupa",
                    Motivo = "La búsqueda global todavía no está implementada."
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Registrar paciente",
                    Icono = "persona-mas",
                    Motivo = "El alta de pacientes todavía no está implementada en el front."
                }
            },
            Panel = new PanelTurnosViewModel
            {
                Titulo = "Turnos programados de hoy",
                Vista = VistaPanel.Administrador,
                Turnos = turnosHoy.Turnos.Select(TurnoFilaViewModel.Desde).ToArray(),
                TextoVacio = "No hay turnos programados para hoy."
            }
        };
    }

    /// <summary>
    /// Por las dudas: hoy la API solo emite tres roles, pero si aparece otro
    /// (secretario, por ejemplo) mostramos algo usable en vez de una pantalla vacía.
    /// </summary>
    private static DashboardViewModel ArmarRolDesconocido(SesionUsuario sesion) => new()
    {
        Rol = sesion.Rol,
        Nombre = sesion.Nombre,
        Aviso = $"No hay un tablero armado para el rol \"{sesion.Rol}\". Mientras tanto podés usar el listado de turnos.",
        Accesos = new[]
        {
            new AccesoRapidoViewModel
            {
                Titulo = "Turnos",
                Icono = "calendario",
                Controlador = "Turnos",
                Accion = "Index"
            }
        }
    };

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
