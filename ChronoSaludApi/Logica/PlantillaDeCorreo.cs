using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace ChronoSaludApi.Logica;

/// <summary>
/// La versión HTML de un correo, armada a partir del texto: así la lógica
/// escribe un solo mensaje y los dos formatos dicen siempre lo mismo. Sigue la
/// marca de la Web (teal sobre arena) con estilos en línea y tablas, que es lo
/// único que respetan todos los clientes de correo. Todo el texto se escapa;
/// las direcciones que empiezan con http se vuelven enlaces.
/// </summary>
public static partial class PlantillaDeCorreo
{
    // Escapa lo que hace falta escapar (<, >, &, comillas) pero deja las
    // letras con tilde como están, en vez de volverlas &#233;.
    private static readonly HtmlEncoder Codificador = HtmlEncoder.Create(UnicodeRanges.All);

    [GeneratedRegex(@"https?://[^\s<>""]+")]
    private static partial Regex Enlaces();

    public static string Html(string asunto, string texto)
    {
        var parrafos = new StringBuilder();
        foreach (var parrafo in Parrafos(texto))
        {
            parrafos.Append("<p style=\"margin:0 0 16px;font-size:16px;line-height:1.5;color:#1B2627;\">")
                    .Append(ConEnlaces(parrafo))
                    .Append("</p>");
        }

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{Codificador.Encode(asunto)}</title></head>
            <body style="margin:0;padding:0;background:#EFE6DA;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#EFE6DA;padding:24px 12px;">
            <tr><td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#FFFFFF;border:1.5px solid #8CC4C9;border-radius:16px;">
            <tr><td style="padding:20px 28px;border-bottom:1px solid #E3D8C8;">
            <span style="display:inline-block;width:28px;height:28px;border-radius:7px;background:#0B6E75;vertical-align:middle;"></span>
            <span style="font-size:18px;font-weight:700;color:#0B6E75;vertical-align:middle;padding-left:8px;">ChronoSalud</span>
            </td></tr>
            <tr><td style="padding:28px;">
            <h1 style="margin:0 0 20px;font-size:20px;line-height:1.3;color:#1B2627;">{Codificador.Encode(asunto)}</h1>
            {parrafos}
            </td></tr>
            </table>
            <p style="max-width:560px;margin:16px auto 0;font-size:12px;line-height:1.5;color:#5A554E;">Este correo lo mandó ChronoSalud automáticamente. No hace falta responderlo.</p>
            </td></tr>
            </table>
            </body>
            </html>
            """;
    }

    /// <summary>Los párrafos van separados por una línea en blanco; dentro de uno, cada salto es un br.</summary>
    private static IEnumerable<string> Parrafos(string texto) =>
        texto.Replace("\r\n", "\n")
             .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ConEnlaces(string parrafo)
    {
        var resultado = new StringBuilder();
        var desde = 0;
        foreach (Match enlace in Enlaces().Matches(parrafo))
        {
            resultado.Append(Escapar(parrafo[desde..enlace.Index]));
            var url = Codificador.Encode(enlace.Value);
            resultado.Append($"<a href=\"{url}\" style=\"color:#0B6E75;font-weight:600;word-break:break-all;\">{url}</a>");
            desde = enlace.Index + enlace.Length;
        }
        resultado.Append(Escapar(parrafo[desde..]));
        return resultado.ToString();
    }

    private static string Escapar(string texto) =>
        Codificador.Encode(texto).Replace("\n", "<br>");
}
