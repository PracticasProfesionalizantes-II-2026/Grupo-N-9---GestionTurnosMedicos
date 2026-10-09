using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ChronoSaludWeb.TagHelpers;

/// <summary>
/// Le pone id al mensaje de error de cada campo (span asp-validation-for),
/// para que CampoConErrorTagHelper lo una al campo. Si la vista ya le puso
/// un id, lo respeta.
/// </summary>
[HtmlTargetElement("span", Attributes = "asp-validation-for")]
public class MensajeDeErrorTagHelper : TagHelper
{
    // Corre después del tag helper de ASP.NET, que arma el mensaje.
    public override int Order => 1000;

    [HtmlAttributeName("asp-validation-for")]
    public ModelExpression For { get; set; } = default!;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes.ContainsName("id"))
            return;

        var nombre = ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);
        output.Attributes.SetAttribute("id", IdDeError.Para(nombre));
    }
}
