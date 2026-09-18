using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class HomeController : Controller
{
    // Cuántos turnos entran en el panel de contexto. La API pagina de a 20.
    private const int TurnosDelPanel = 6;

    // Cuántas consultas y recetas entran en el historial reciente del paciente.
    private const int ActividadDelPanel = 3;

    private readonly AuthService _auth;
    private readonly TurnoService _turnos;
    private readonly PacienteService _pacientes;
    private readonly DoctorService _doctores;
    private readonly HistorialService _historial;
    private readonly RecetaService _recetas;

    public HomeController(
        AuthService auth,
        TurnoService turnos,
        PacienteService pacientes,
        DoctorService doctores,
        HistorialService historial,
        RecetaService recetas)
    {
        _auth = auth;
        _turnos = turnos;
        _pacientes = pacientes;
        _doctores = doctores;
        _historial = historial;
        _recetas = recetas;
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
    /// El perfil de paciente (para conocer el IdPaciente, que no es el
    /// IdUsuario), sus turnos de hoy en adelante y su historial reciente.
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
            Disposicion = DisposicionDashboard.ActividadDerecha,
            // El registro público (CuentaController.Registro) siempre crea la
            // fila en Pacientes, así que "perfil is null" a esta altura es un
            // caso residual: un Usuario rol paciente cargado por otra vía. Si
            // la fila existe pero está vacía, lo que falta es la ficha clínica.
            Aviso = perfil switch
            {
                null                                  => "Tu usuario todavía no tiene un perfil de paciente asociado, así que no podemos mostrar tus turnos. Pedile a la administración que lo cree.",
                { FechaNacimiento: null }              => "Todavía no completaste tu ficha clínica.",
                _                                      => null
            },
            AvisoEnlace = perfil is { FechaNacimiento: null }
                ? new EnlaceViewModel
                {
                    Controlador = "MiPerfil",
                    Accion = "CompletarPaciente",
                    Descripcion = "Completar mi perfil"
                }
                : null,
            Accesos = new[]
            {
                new AccesoRapidoViewModel
                {
                    Titulo = "Agendar turno",
                    Icono = "calendario-mas",
                    Controlador = "Turnos",
                    Accion = "Crear"
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Historial consultas",
                    Icono = "historia",
                    Controlador = "Historial",
                    Accion = "Index"
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Recetas",
                    Icono = "pastilla",
                    Controlador = "Recetas",
                    Accion = "Index"
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
                TextoVacio = "No tenés turnos programados de hoy en adelante.",
                VerMas = new EnlaceViewModel
                {
                    Controlador = "Turnos",
                    Accion = "Index",
                    Descripcion = "Ver todos mis turnos"
                }
            },
            Actividad = await ArmarActividadAsync(perfil?.IdPaciente)
        };
    }

    /// <summary>
    /// El historial reciente del paciente: sus consultas y sus recetas en una
    /// sola lista, de la más nueva a la más vieja.
    /// </summary>
    private async Task<PanelActividadViewModel> ArmarActividadAsync(int? idPaciente)
    {
        var panel = new PanelActividadViewModel
        {
            Titulo = "Historial reciente",
            TextoVacio = "Todavía no hay consultas ni recetas registradas.",
            VerMas = new EnlaceViewModel
            {
                Controlador = "Historial",
                Accion = "Index",
                Descripcion = "Ver toda mi historia clínica"
            }
        };

        if (idPaciente is null) return panel;

        try
        {
            // Van una después de la otra y no en paralelo porque el ApiClient lee
            // el token de HttpContext.Session, que no es seguro en concurrencia.
            var consultas = await _historial.ObtenerDePacienteAsync(idPaciente.Value);
            var recetas = await _recetas.ObtenerDePacienteAsync(idPaciente.Value);

            var entradas = consultas
                .Select(c => new ActividadRecienteViewModel { Tipo = "Consulta", Fecha = c.Fecha })
                .Concat(recetas.Select(r => new ActividadRecienteViewModel { Tipo = "Receta", Fecha = r.Fecha }))
                .OrderByDescending(e => e.Fecha)
                .ThenBy(e => e.Tipo, StringComparer.Ordinal)
                .Take(ActividadDelPanel)
                .ToArray();

            return new PanelActividadViewModel
            {
                Titulo = panel.Titulo,
                TextoVacio = panel.TextoVacio,
                VerMas = panel.VerMas,
                Entradas = entradas
            };
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            // Que falle el historial no tiene por qué voltear todo el tablero:
            // los turnos y los accesos siguen sirviendo. El 401 sí se deja pasar,
            // lo maneja ApiExceptionFilter mandando al login.
            return new PanelActividadViewModel
            {
                Titulo = panel.Titulo,
                TextoVacio = panel.TextoVacio,
                VerMas = panel.VerMas,
                Error = $"No se pudo cargar el historial reciente: {error.Message}"
            };
        }
    }

    /// <summary>
    /// Dos llamadas: el perfil de doctor (para el IdDoctor) y sus próximos turnos.
    /// </summary>
    private async Task<DashboardViewModel> ArmarDoctorAsync(SesionUsuario sesion)
    {
        var perfil = await _doctores.ObtenerMiPerfilAsync();

        var turnos = perfil is null
            ? Array.Empty<TurnoFilaViewModel>()
            : (await _turnos.ObtenerAsync(
                    doctorId: perfil.IdDoctor,
                    desde: DateTime.Today,
                    limite: TurnosDelPanel))
                .Turnos.Select(TurnoFilaViewModel.Desde).ToArray();

        var hoy = DateTime.Today.ToString("yyyy-MM-dd");

        // El login solo devuelve el nombre de pila, pero /doctores/me ya nos
        // trajo el apellido, así que el saludo sale sin pedir nada más.
        var nombre = perfil is null
            ? sesion.Nombre
            : $"{perfil.Nombre} {perfil.Apellido}".Trim();

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = nombre,
            Disposicion = DisposicionDashboard.PanelAbajo,
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
                    Controlador = "Historial",
                    Accion = "Index"
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Recetas",
                    Icono = "pastilla",
                    Controlador = "Recetas",
                    Accion = "Index"
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
                Titulo = "Próximos turnos",
                Vista = VistaPanel.Doctor,
                Turnos = turnos,
                TextoVacio = "No tenés turnos agendados de hoy en adelante.",
                VerMas = new EnlaceViewModel
                {
                    Controlador = "Turnos",
                    Accion = "Index",
                    Descripcion = "Ver toda mi agenda"
                }
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

        var hoy = DateTime.Today.ToString("yyyy-MM-dd");
        var soloHoy = new Dictionary<string, string> { ["desde"] = hoy, ["hasta"] = hoy };

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = sesion.Nombre,
            Metricas = new[]
            {
                new MetricaViewModel
                {
                    Titulo = "Turnos de hoy",
                    Valor = turnosHoy.Total,
                    Icono = "calendario",
                    VerMas = new EnlaceViewModel
                    {
                        Controlador = "Turnos",
                        Accion = "Index",
                        Ruta = soloHoy,
                        Descripcion = "Ver los turnos de hoy"
                    }
                },
                new MetricaViewModel
                {
                    Titulo = "Pacientes",
                    Valor = totalPacientes,
                    Icono = "personas",
                    VerMas = new EnlaceViewModel
                    {
                        Controlador = "Pacientes",
                        Accion = "Index",
                        Descripcion = "Ver todos los pacientes"
                    }
                }
            },
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
                    // Es la acción más repetitiva del admin: es quien confirma
                    // los turnos que van quedando pendientes.
                    Titulo = "Turnos pendientes",
                    Icono = "calendario",
                    Controlador = "Turnos",
                    Accion = "Index",
                    Ruta = new Dictionary<string, string> { ["estado"] = "pendiente" }
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Pacientes",
                    Icono = "personas",
                    Controlador = "Pacientes",
                    Accion = "Index"
                },
                new AccesoRapidoViewModel
                {
                    // Hub de gestión de cuentas: reemplaza al alta directa de
                    // doctor, que ahora vive reorganizada ahí adentro.
                    Titulo = "Cuentas",
                    Icono = "persona-mas",
                    Controlador = "Admin",
                    Accion = "Index"
                }
            },
            Panel = new PanelTurnosViewModel
            {
                Titulo = "Turnos programados",
                Vista = VistaPanel.Administrador,
                Turnos = turnosHoy.Turnos.Select(TurnoFilaViewModel.Desde).ToArray(),
                TextoVacio = "No hay turnos programados para hoy.",
                VerMas = new EnlaceViewModel
                {
                    Controlador = "Turnos",
                    Accion = "Index",
                    Ruta = soloHoy,
                    Descripcion = "Ver todos los turnos de hoy"
                }
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
