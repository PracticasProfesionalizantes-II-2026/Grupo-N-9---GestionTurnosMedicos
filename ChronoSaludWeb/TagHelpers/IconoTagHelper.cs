using ChronoSaludWeb.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ChronoSaludWeb.TagHelpers;

/// <summary>
/// &lt;icono nombre="volver" class="size-4" /&gt; dibuja un ícono del catálogo
/// (<see cref="IconosLucide"/>) con los atributos de siempre: trazo 1,5, sin
/// relleno, del color del texto y oculto para los lectores de pantalla.
/// Antes cada vista pegaba el &lt;svg&gt; entero; así el trazo vive en un solo
/// lugar. La clase la escribe la vista, para que Tailwind la encuentre.
/// </summary>
[HtmlTargetElement("icono", Attributes = "nombre", TagStructure = TagStructure.WithoutEndTag)]
public class IconoTagHelper : TagHelper
{
    public string Nombre { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;

        // La clase y cualquier otro atributo que traiga (por ejemplo
        // stroke-width) quedan como los escribió la vista.
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        if (!output.Attributes.ContainsName("stroke-width"))
            output.Attributes.SetAttribute("stroke-width", "1.5");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Attributes.SetAttribute("aria-hidden", "true");

        // Trazos es un switch sobre literales del código: es seguro sin
        // codificar.
        output.Content.SetHtmlContent(IconosLucide.Trazos(Nombre));
    }
}
