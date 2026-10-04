using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class DoctoresController : ControladorBase
{
    private const int Limite = 100;

    private readonly DoctorService _doctores;
    private readonly UsuarioService _usuarios;
    private readonly AuthService _auth;

    public DoctoresController(DoctorService doctores, UsuarioService usuarios, AuthService auth)
    {
        _doctores = doctores;
        _usuarios = usuarios;
        _auth = auth;
    }

    private const string TituloSinPermiso = "No podés ver los doctores con tu rol";

    // Hoy PuedeVerDoctores es true para cualquier sesión (GET /doctores no le
    // exige rol a la API), así que este mensaje es inalcanzable en la práctica.
    // Se deja el guard igual que en PacientesController para que, si el día de
    // mañana la API restringe el endpoint, el cambio sea solo en AuthService.
    private const string MotivoSinPermiso =
        "Tu usuario no tiene permiso para ver el padrón de doctores.";

    public async Task<IActionResult> Index(string? especialidad)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        if (!_auth.PuedeVerDoctores)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        var filtro = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim();

        try
        {
            var especialidades = await _doctores.ObtenerEspecialidadesAsync();
            var pagina = await _doctores.BuscarAsync(filtro, Limite);

            // Un solo pedido para toda la página: quiénes tienen foto y cuál es
            // su IdUsuario, que el listado de doctores no trae.
            var fotos = await _usuarios.ObtenerFotosAsync(doctores: pagina.Doctores.Select(d => d.IdDoctor));

            return View(new DoctoresIndexViewModel
            {
                Especialidad = filtro,
                Total = pagina.Total,
                Doctores = pagina.Doctores.Select(d => Mapear(d, UrlDeFoto(fotos, d.IdDoctor))).ToList(),
                Especialidades = OpcionesDeEspecialidad(especialidades, filtro)
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new DoctoresIndexViewModel { Especialidad = filtro, Error = error.Message });
        }
    }

    public async Task<IActionResult> Detalle(int id)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Detalle), new { id }));

        if (!_auth.PuedeVerDoctores)
            return SinPermiso(TituloSinPermiso, MotivoSinPermiso);

        try
        {
            var doctor = await _doctores.ObtenerPorIdAsync(id);

            if (doctor is null)
                return NoEncontrado(id, "Doctor no encontrado", "doctor");

            // El horario va aparte: si falla, la ficha se muestra igual con
            // el aviso en su tarjeta en vez de perder todos los datos.
            HorarioSemanalViewModel? horario = null;
            string? errorHorario = null;
            try
            {
                var horarios = await _doctores.ObtenerHorariosAsync(id);
                horario = HorarioSemanalViewModel.Desde(horarios ?? Array.Empty<HorarioLaboral>());
            }
            catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
            {
                errorHorario = error.Message;
            }

            var fotos = await _usuarios.ObtenerFotosAsync(doctores: new[] { doctor.IdDoctor });

            return View(new DoctorDetalleViewModel
            {
                IdDoctor = doctor.IdDoctor,
                FotoUrl = UrlDeFoto(fotos, doctor.IdDoctor),
                Nombre = doctor.Nombre,
                Apellido = doctor.Apellido,
                Especialidad = doctor.Especialidad,
                Matricula = doctor.Matricula,
                Consultorio = doctor.Consultorio,
                Horario = horario,
                ErrorHorario = errorHorario,
                PuedePedirTurno = _auth.PuedeCargarTurnos
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new DoctorDetalleViewModel { IdDoctor = id, Error = error.Message });
        }
    }

    /// <summary>
    /// /Usuarios/Foto/{idUsuario} si la API informó que ese doctor tiene foto;
    /// null si no, así el avatar no pide una imagen que no existe.
    /// </summary>
    private string? UrlDeFoto(FotosDisponibles fotos, int idDoctor) =>
        fotos.Doctores.TryGetValue(idDoctor, out var idUsuario)
            ? Url.Action("Foto", "Usuarios", new { id = idUsuario })
            : null;

    private static DoctorFilaViewModel Mapear(DoctorLista doctor, string? fotoUrl) => new()
    {
        FotoUrl = fotoUrl,
        IdDoctor = doctor.IdDoctor,
        Nombre = doctor.Nombre,
        Especialidad = doctor.Especialidad,
        Matricula = doctor.Matricula
    };

    /// <summary>
    /// Opciones del select, desde GET /doctores/especialidades (ya vienen sin
    /// repetir y ordenadas por la API).
    /// </summary>
    private static IReadOnlyList<SelectListItem> OpcionesDeEspecialidad(
        IEnumerable<string> especialidades, string? seleccionada) =>
        especialidades
            .Select(e => new SelectListItem(e, e, string.Equals(e, seleccionada, StringComparison.OrdinalIgnoreCase)))
            .ToList();
}
