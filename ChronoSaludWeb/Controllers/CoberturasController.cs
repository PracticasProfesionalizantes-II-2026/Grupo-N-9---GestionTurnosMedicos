using Microsoft.AspNetCore.Mvc;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Controllers;

public class CoberturasController : ControladorBase
{
    private readonly CoberturaService _coberturas;
    private readonly AuthService _auth;

    public CoberturasController(CoberturaService coberturas, AuthService auth)
    {
        _coberturas = coberturas;
        _auth = auth;
    }

    /// <summary>
    /// El catálogo general de coberturas. La API no lo restringe por rol
    /// (a diferencia de la cobertura de un paciente puntual), así que
    /// alcanza con tener sesión.
    /// </summary>
    public async Task<IActionResult> Index(string? nombre)
    {
        if (!_auth.HaySesion)
            return AlLogin(Url.Action(nameof(Index), new { nombre }));

        var busqueda = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();

        try
        {
            var coberturas = await _coberturas.ObtenerTodasAsync(busqueda);

            return View(new CoberturasIndexViewModel
            {
                Nombre = busqueda,
                Coberturas = coberturas
                    .Select(c => new CoberturaCatalogoFilaViewModel { Nombre = c.Nombre, Plan = c.Plan })
                    .OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray()
            });
        }
        catch (ApiException error) when (error.Status != StatusCodes.Status401Unauthorized)
        {
            return View(new CoberturasIndexViewModel { Nombre = busqueda, Error = error.Message });
        }
    }
}
