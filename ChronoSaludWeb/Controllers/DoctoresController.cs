using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class DoctoresController : ControladorBase
{
    private const int Limite = 100;

    private readonly DoctorService _doctores;
    private readonly AuthService _auth;

    public DoctoresController(DoctorService doctores, AuthService auth)
    {
        _doctores = doctores;
        _auth = auth;
    }

    public async Task<IActionResult> Index(string? especialidad)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index)));

        var filtro = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim();

        try
        {
            // La API no expone un endpoint de especialidades, así que las
            // opciones del select salen de la lista completa de doctores.
            var todos = await _doctores.BuscarAsync(limite: 200);

            // Sin filtro no hace falta un segundo pedido: sirve la misma lista.
            var pagina = filtro is null
                ? todos
                : await _doctores.BuscarAsync(filtro, Limite);

            return View(new DoctoresIndexViewModel
            {
                Especialidad = filtro,
                Total = pagina.Total,
                Doctores = pagina.Doctores.Select(Mapear).ToList(),
                Especialidades = OpcionesDeEspecialidad(todos.Doctores, filtro)
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

        try
        {
            var doctor = await _doctores.ObtenerPorIdAsync(id);

            if (doctor is null)
                return NoEncontrado(id, "Doctor no encontrado", "doctor");

            return View(new DoctorDetalleViewModel
            {
                IdDoctor = doctor.IdDoctor,
                Nombre = doctor.Nombre,
                Apellido = doctor.Apellido,
                Especialidad = doctor.Especialidad,
                Matricula = doctor.Matricula,
                Consultorio = doctor.Consultorio
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new DoctorDetalleViewModel { IdDoctor = id, Error = error.Message });
        }
    }

    private static DoctorFilaViewModel Mapear(DoctorLista doctor) => new()
    {
        IdDoctor = doctor.IdDoctor,
        Nombre = doctor.Nombre,
        Especialidad = doctor.Especialidad,
        Matricula = doctor.Matricula
    };

    private static IReadOnlyList<SelectListItem> OpcionesDeEspecialidad(
        IReadOnlyList<DoctorLista> doctores, string? seleccionada) =>
        doctores
            .Select(d => d.Especialidad?.Trim())
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => new SelectListItem(e, e, string.Equals(e, seleccionada, StringComparison.OrdinalIgnoreCase)))
            .ToList();
}
