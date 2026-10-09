using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ChronoSaludWeb.TagHelpers;

/// <summary>
/// Une cada campo con su mensaje de error para el lector de pantalla. Si el
/// servidor devolvió un error para el campo:
/// - le pone aria-invalid="true";
/// - suma a su aria-describedby el id del mensaje (sin perder la ayuda que ya tenía);
/// - el primer campo con error de la página recibe el foco (autofocus).
/// Sin error no toca nada. Actúa sobre todo input, select y textarea con asp-for.
/// Si la vista ya trae un autofocus escrito (Login, Registro) y algún campo
/// volvió con error, ese autofocus se saca: el foco va al campo con error y
/// nunca quedan dos en la página.
/// </summary>
[HtmlTargetElement("input", Attributes = "asp-for")]
[HtmlTargetElement("select", Attributes = "asp-for")]
[HtmlTargetElement("textarea", Attributes = "asp-for")]
public class CampoConErrorTagHelper : TagHelper
{
    // Marca en el pedido de que ya hay un campo con el foco.
    private const string ClaveFoco = "chronosalud.focoEnError";

    // Corre después de los tag helpers de ASP.NET, que arman el campo.
    public override int Order => 1000;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = default!;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes.ContainsName("autofocus") && HayErroresDeCampo(ViewContext.ModelState))
            output.Attributes.RemoveAll("autofocus");

        // Un campo oculto no se ve ni recibe el foco.
        if (output.Attributes["type"]?.Value?.ToString() == "hidden")
            return;

        var nombre = ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);
        if (!TieneError(ViewContext.ModelState, nombre))
            return;

        output.Attributes.SetAttribute("aria-invalid", "true");

        var descripcion = output.Attributes["aria-describedby"]?.Value?.ToString();
        output.Attributes.SetAttribute("aria-describedby", SumarId(descripcion, IdDeError.Para(nombre)));

        if (!ViewContext.HttpContext.Items.ContainsKey(ClaveFoco))
        {
            ViewContext.HttpContext.Items[ClaveFoco] = true;
            output.Attributes.SetAttribute("autofocus", "autofocus");
        }
    }

    public static bool TieneError(ModelStateDictionary estado, string nombre)
        => estado.TryGetValue(nombre, out var entrada) && entrada.Errors.Count > 0;

    /// <summary>
    /// Algún campo tiene error. Los errores generales (clave vacía, como
    /// "Email o contraseña incorrectos") no cuentan: no son de un campo.
    /// </summary>
    public static bool HayErroresDeCampo(ModelStateDictionary estado)
    {
        foreach (var (clave, entrada) in estado)
        {
            if (clave != string.Empty && entrada.Errors.Count > 0)
                return true;
        }
        return false;
    }

    /// <summary>"ayuda-nueva" + "error-Nueva" → "ayuda-nueva error-Nueva".</summary>
    public static string SumarId(string? ids, string id)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return id;

        return ids.Trim() + " " + id;
    }
}
