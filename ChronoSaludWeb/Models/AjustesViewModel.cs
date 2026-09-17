namespace ChronoSaludWeb.Models;

/// <summary>
/// Una opción del formulario de ajustes. El Valor es el que viaja a la cookie
/// y termina en el &lt;html&gt;; el Titulo y la Descripcion son solo para la
/// pantalla.
/// </summary>
public record OpcionAjuste(string Valor, string Titulo, string Descripcion);

/// <summary>
/// Preferencias visuales del formulario de Ajustes. No hay validaciones con
/// DataAnnotations porque los valores no se escriben a mano: son radios con un
/// conjunto cerrado de opciones, y el controlador igual los pasa por la lista
/// blanca antes de guardarlos.
/// </summary>
public class AjustesViewModel
{
    public string Tema { get; set; } = "light";

    public string Escala { get; set; } = "md";

    /// <summary>
    /// Pantalla desde la que el usuario abrió Ajustes, para devolverlo ahí
    /// después de guardar. Se completa en el GET porque en el POST el referer
    /// ya es la propia pantalla de Ajustes.
    /// </summary>
    public string? UrlRetorno { get; set; }

    public static readonly OpcionAjuste[] Temas =
    [
        new("light", "Claro", "El aspecto habitual, con fondo claro."),
        new("dark", "Oscuro", "Fondo oscuro, más cómodo con poca luz.")
    ];

    public static readonly OpcionAjuste[] Escalas =
    [
        new("md", "Normal", "El tamaño de texto por defecto."),
        new("lg", "Grande", "Un 12% más grande, con el resto de la pantalla en proporción."),
        new("xl", "Extra grande", "Un 25% más grande, para leer de lejos o con baja visión.")
    ];
}
