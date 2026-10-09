using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChronoSaludWeb.TagHelpers;

/// <summary>
/// El id del mensaje de error de un campo. Lo usan los dos tag helpers y las
/// vistas que arman el mensaje a mano (ErrorCampo), así siempre coinciden.
/// </summary>
public static class IdDeError
{
    /// <summary>
    /// "Cuenta.Email" → "error-Cuenta_Email". Va con prefijo y no con sufijo
    /// porque jQuery Validation ya usa "Cuenta_Email-error" para el mensaje
    /// que arma en el navegador, y dos ids iguales no pueden convivir.
    /// </summary>
    public static string Para(string nombreDelCampo)
        => "error-" + TagBuilder.CreateSanitizedId(nombreDelCampo, "_");
}
