using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChronoSaludWeb.Models;

/// <summary>
/// Las listas fijas de la ficha del paciente. La API no tiene catálogos para
/// estos datos, así que se definen acá una sola vez y las usan el alta
/// (Pacientes/Crear) y la edición (Usuarios/Editar).
/// </summary>
public static class OpcionesPaciente
{
    public static readonly string[] TiposDocumento = { "DNI", "LC", "LE", "Pasaporte" };

    // En minúscula, igual que lo guarda Mi perfil. Las vistas los muestran
    // con mayúscula inicial (clase "capitalize").
    public static readonly string[] Sexos = { "femenino", "masculino", "otro" };

    public static readonly string[] GruposSanguineos = { "A+", "A-", "B+", "B-", "AB+", "AB-", "0+", "0-" };

    public static readonly string[] EstadosCiviles =
        { "Soltero/a", "Casado/a", "Divorciado/a", "Viudo/a", "Unión convivencial" };

    public static readonly string[] Provincias =
    {
        "Buenos Aires",
        "Catamarca",
        "Chaco",
        "Chubut",
        "Ciudad Autónoma de Buenos Aires",
        "Córdoba",
        "Corrientes",
        "Entre Ríos",
        "Formosa",
        "Jujuy",
        "La Pampa",
        "La Rioja",
        "Mendoza",
        "Misiones",
        "Neuquén",
        "Río Negro",
        "Salta",
        "San Juan",
        "San Luis",
        "Santa Cruz",
        "Santa Fe",
        "Santiago del Estero",
        "Tierra del Fuego, Antártida e Islas del Atlántico Sur",
        "Tucumán"
    };

    /// <summary>
    /// Arma las opciones de un select. Si el valor guardado no está en la
    /// lista (la API acepta cualquier texto), se agrega al final, para que el
    /// select lo muestre en vez de aparentar que el dato está vacío.
    /// </summary>
    public static List<SelectListItem> Opciones(string[] valores, string? actual)
    {
        var opciones = new List<SelectListItem>();

        foreach (var valor in valores)
        {
            opciones.Add(new SelectListItem(valor, valor));
        }

        if (!string.IsNullOrWhiteSpace(actual) && !valores.Contains(actual))
        {
            opciones.Add(new SelectListItem(actual, actual));
        }

        return opciones;
    }
}
