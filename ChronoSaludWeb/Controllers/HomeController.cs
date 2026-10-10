using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Filters;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

// Sin sesión el inicio muestra la portada; privacidad y error son públicas.
[PermiteSinSesion]
public class HomeController : Controller
{
    // Cuántos turnos entran en el panel de contexto. Es también lo que se le
    // pide a la API: ella filtra los estados y ordena.
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
            : Filas(await TurnosEnPieAsync(pacienteId: perfil.IdPaciente, desde: FechaArgentina.Hoy()));

        var agendar = new EnlaceViewModel
        {
            Controlador = "Turnos",
            Accion = "Crear",
            Descripcion = "Agendar turno"
        };

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = sesion.Nombre,
            Disposicion = DisposicionDashboard.ActividadDerecha,
            // Sin perfil no hay turnos que resumir (lo explica el aviso de
            // abajo): el banner queda solo con el saludo.
            Banner = perfil is null
                ? null
                : ArmarBanner(
                    Proximo(turnos),
                    turno => turno.Doctor,
                    sinTurnos: "No tenés turnos programados.",
                    principal: agendar,
                    secundario: new EnlaceViewModel
                    {
                        Controlador = "Turnos",
                        Accion = "Index",
                        Descripcion = "Ver mis turnos"
                    }),
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
                    Accion = "Editar",
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
                    Titulo = "Historial de consultas",
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
                    Titulo = "Mis estudios",
                    Icono = "estudio",
                    Controlador = "Estudios",
                    Accion = "Index"
                },
                new AccesoRapidoViewModel
                {
                    Titulo = "Mis coberturas",
                    Icono = "escudo",
                    Motivo = "Todavía no podés ver tus coberturas desde acá."
                }
            },
            Panel = new PanelTurnosViewModel
            {
                Titulo = "Próximos turnos",
                Vista = VistaPanel.Paciente,
                Turnos = turnos.Take(TurnosDelPanel).ToArray(),
                TextoVacio = perfil is null
                    ? "No hay turnos para mostrar"
                    : "No tenés turnos programados",
                AyudaVacio = perfil is null
                    ? null
                    : "Elegí especialidad, doctor y horario en pocos pasos.",
                AccionVacio = perfil is null ? null : agendar,
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
            TextoVacio = "Todavía no tenés consultas ni recetas",
            AyudaVacio = "Cuando un doctor te atienda, vas a ver acá lo último de tu historia clínica.",
            AccionVacio = new EnlaceViewModel
            {
                Controlador = "Doctores",
                Accion = "Index",
                Descripcion = "Ver doctores"
            },
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
                AyudaVacio = panel.AyudaVacio,
                AccionVacio = panel.AccionVacio,
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
            : Filas(await TurnosEnPieAsync(doctorId: perfil.IdDoctor, desde: FechaArgentina.Hoy()));

        var hoy = FechaArgentina.Hoy().ToString("yyyy-MM-dd");

        // Sin perfil no hay agenda: el aviso de abajo explica eso y no este.
        var sinCerrar = perfil is null ? null : TurnosSinCerrar.Aviso(await ContarSinCerrarAsync(perfil.IdDoctor), esDoctor: true);

        var todaLaAgenda = new EnlaceViewModel
        {
            Controlador = "Turnos",
            Accion = "Index",
            Descripcion = "Ver toda mi agenda"
        };

        // El login solo devuelve el nombre de pila, pero /doctores/me ya nos
        // trajo el apellido, así que el saludo sale sin pedir nada más.
        var nombre = perfil is null
            ? sesion.Nombre
            : $"{perfil.Nombre} {perfil.Apellido}".Trim();

        var accesos = new List<AccesoRapidoViewModel>
        {
            new()
            {
                Titulo = "Turnos del día",
                Icono = "calendario",
                Controlador = "Turnos",
                Accion = "Index",
                Ruta = new Dictionary<string, string> { ["desde"] = hoy, ["hasta"] = hoy }
            },
            new()
            {
                Titulo = "Historia clínica",
                Icono = "historia",
                Controlador = "Historial",
                Accion = "Index"
            },
            new()
            {
                Titulo = "Recetas",
                Icono = "pastilla",
                Controlador = "Recetas",
                Accion = "Index"
            },
            new()
            {
                Titulo = "Coberturas",
                Icono = "escudo",
                Controlador = "Coberturas",
                Accion = "Index"
            }
        };

        // Sin perfil no hay horario que editar ni actividad que mostrar. El id
        // es el de /doctores/me, y la acción Horario vuelve a revisar que sea
        // el propio.
        if (perfil is not null)
        {
            accesos.Add(new AccesoRapidoViewModel
            {
                Titulo = "Mi horario",
                Icono = "reloj",
                Controlador = "Doctores",
                Accion = "Horario",
                Ruta = new Dictionary<string, string> { ["id"] = $"{perfil.IdDoctor}" }
            });
            accesos.Add(new AccesoRapidoViewModel
            {
                Titulo = "Mi actividad",
                Icono = "actividad",
                Controlador = "Actividad",
                Accion = "Mia"
            });
        }

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = nombre,
            Disposicion = DisposicionDashboard.PanelAbajo,
            // Sin perfil no hay turnos que resumir (lo explica el aviso):
            // el banner queda solo con el saludo.
            Banner = perfil is null
                ? null
                : ArmarBanner(
                    Proximo(turnos),
                    turno => turno.Paciente,
                    sinTurnos: "No tenés turnos agendados de hoy en adelante.",
                    principal: new EnlaceViewModel
                    {
                        Controlador = "Turnos",
                        Accion = "Index",
                        Ruta = new Dictionary<string, string> { ["desde"] = hoy, ["hasta"] = hoy },
                        Descripcion = "Ver turnos de hoy"
                    },
                    secundario: todaLaAgenda),
            Aviso = perfil is null
                ? "Tu cuenta todavía no tiene el perfil de doctor completo (matrícula y especialidad), por eso no vemos tus turnos. Pedile a administración que lo complete."
                : sinCerrar,
            AvisoEnlace = sinCerrar is null ? null : TurnosSinCerrar.Enlace,
            Accesos = accesos,
            Panel = new PanelTurnosViewModel
            {
                Titulo = "Próximos turnos",
                Vista = VistaPanel.Doctor,
                Turnos = turnos.Take(TurnosDelPanel).ToArray(),
                TextoVacio = perfil is null
                    ? "No hay turnos para mostrar"
                    : "No tenés turnos agendados de hoy en adelante",
                AccionVacio = perfil is null ? null : todaLaAgenda,
                VerMas = todaLaAgenda
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
        // Solo los que siguen en pie: un turno cancelado o ya completado no
        // es un turno "programado" para hoy.
        var diaDeHoy = FechaArgentina.Hoy();
        var enPieHoy = await TurnosEnPieAsync(desde: diaDeHoy, hasta: diaDeHoy);

        // El panel muestra los primeros; la métrica y el saludo cuentan todos.
        var turnosHoy = Filas(enPieHoy);
        var cantidadHoy = enPieHoy.Total;

        var totalPacientes = await _pacientes.ContarAsync();

        var sinCerrar = TurnosSinCerrar.Aviso(await ContarSinCerrarAsync(doctorId: null), esDoctor: false);

        var hoy = diaDeHoy.ToString("yyyy-MM-dd");
        var soloHoy = new Dictionary<string, string> { ["desde"] = hoy, ["hasta"] = hoy };

        var nuevoTurno = new EnlaceViewModel
        {
            Controlador = "Turnos",
            Accion = "Crear",
            Descripcion = "Nuevo turno"
        };

        return new DashboardViewModel
        {
            Rol = sesion.Rol,
            Nombre = sesion.Nombre,
            Aviso = sinCerrar,
            AvisoEnlace = sinCerrar is null ? null : TurnosSinCerrar.Enlace,
            // Sale del total de hoy que ya se pide para la métrica y el panel.
            Banner = new BannerViewModel
            {
                Resumen = cantidadHoy == 0 ? "Hoy no hay turnos programados." : "Hoy hay",
                Destacado = cantidadHoy switch
                {
                    0 => null,
                    1 => "1 turno programado",
                    _ => $"{cantidadHoy} turnos programados"
                },
                Cierre = cantidadHoy == 0 ? null : ".",
                Principal = nuevoTurno,
                Secundario = new EnlaceViewModel
                {
                    Controlador = "Turnos",
                    Accion = "Index",
                    Ruta = new Dictionary<string, string> { ["estado"] = "pendiente" },
                    Descripcion = "Ver turnos pendientes"
                }
            },
            Metricas = new[]
            {
                new MetricaViewModel
                {
                    Titulo = "Turnos de hoy",
                    Valor = cantidadHoy,
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
                },
                new AccesoRapidoViewModel
                {
                    // Quién cambió horarios y turnos: acá ve el aviso de que
                    // un doctor tocó su horario.
                    Titulo = "Actividad",
                    Icono = "actividad",
                    Controlador = "Actividad",
                    Accion = "Index"
                }
            },
            Panel = new PanelTurnosViewModel
            {
                Titulo = "Turnos programados",
                Vista = VistaPanel.Administrador,
                Turnos = turnosHoy.Take(TurnosDelPanel).ToArray(),
                TextoVacio = "No hay turnos programados para hoy",
                AccionVacio = nuevoTurno,
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
    /// Los turnos que siguen en pie (pendientes y confirmados) para ese filtro:
    /// los primeros TurnosDelPanel, ordenados por día y hora, y en Total
    /// cuántos son en total. Los cancelados y los completados no cuentan como
    /// programados ni como próximos. El filtro y el orden los hace la API.
    /// </summary>
    private async Task<TurnosPagina> TurnosEnPieAsync(
        DateTime desde, DateTime? hasta = null, int? pacienteId = null, int? doctorId = null)
    {
        return await _turnos.ObtenerAsync(
            pacienteId: pacienteId,
            doctorId: doctorId,
            estados: "pendiente,confirmado",
            desde: desde,
            hasta: hasta,
            orden: "fecha",
            limite: TurnosDelPanel);
    }

    /// <summary>
    /// Cuántos turnos de días anteriores siguen pendientes o confirmados. Se
    /// pide con limite=1 para quedarse solo con el total. Si la API falla
    /// devuelve null: el aviso no sale y el inicio se muestra igual.
    /// </summary>
    private async Task<int?> ContarSinCerrarAsync(int? doctorId)
    {
        try
        {
            var pagina = await _turnos.ObtenerAsync(
                doctorId: doctorId,
                estados: TurnosSinCerrar.Estados,
                hasta: TurnosSinCerrar.Hasta(FechaArgentina.Hoy()),
                limite: 1);
            return pagina.Total;
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return null;
        }
    }

    /// <summary>Las filas del panel a partir de lo que devolvió la API.</summary>
    private static TurnoFilaViewModel[] Filas(TurnosPagina pagina) =>
        pagina.Turnos.Select(TurnoFilaViewModel.Desde).ToArray();

    /// <summary>El primer turno de la lista, que ya viene ordenada y solo con los que siguen en pie.</summary>
    private static TurnoFilaViewModel? Proximo(IEnumerable<TurnoFilaViewModel> turnos) =>
        turnos.FirstOrDefault();

    /// <summary>
    /// "hoy a las 10:30", "mañana a las 9:00" o "el jueves 8 de octubre a las
    /// 10:30". No se compara la hora con la actual: el servidor y la clínica
    /// pueden estar en husos distintos, así que un turno de hoy cuenta como
    /// próximo durante todo el día.
    /// </summary>
    private static string Cuando(TurnoFilaViewModel turno)
    {
        var fecha = turno.FechaInicio.Date;

        var hoy = FechaArgentina.Hoy();

        var dia = fecha == hoy ? "hoy"
            : fecha == hoy.AddDays(1) ? "mañana"
            : "el " + fecha.ToString("dddd d 'de' MMMM", TurnosIndexViewModel.Cultura);

        return turno.Hora is null ? dia : $"{dia} a las {turno.Hora}";
    }

    /// <summary>
    /// La frase del banner para paciente y doctor: el próximo turno y con
    /// quién, o el texto de "sin turnos". <paramref name="conQuien"/> devuelve
    /// el nombre crudo de la otra persona; si viene vacío se omite.
    /// </summary>
    private static BannerViewModel ArmarBanner(
        TurnoFilaViewModel? proximo,
        Func<TurnoFilaViewModel, string> conQuien,
        string sinTurnos,
        EnlaceViewModel principal,
        EnlaceViewModel secundario)
    {
        if (proximo is null)
            return new BannerViewModel { Resumen = sinTurnos, Principal = principal, Secundario = secundario };

        var persona = conQuien(proximo).Trim();

        return new BannerViewModel
        {
            Resumen = "Tu próximo turno es",
            Destacado = Cuando(proximo),
            Cierre = persona.Length == 0 ? "." : $" con {persona}.",
            Principal = principal,
            Secundario = secundario
        };
    }

    /// <summary>
    /// Por las dudas: hoy la API solo emite tres roles, pero si aparece otro
    /// mostramos algo usable en vez de una pantalla vacía.
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
