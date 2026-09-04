using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ChronoSaludWeb.Services;

namespace ChronoSaludWeb.Filters;

/// <summary>
/// Red de seguridad para los errores de la API. Evita repetir el mismo try/catch
/// en cada acción: si la sesión venció manda al login, y cualquier otro error
/// deja el mensaje en TempData para que lo muestre el layout.
/// Las acciones que quieran mostrar el error dentro de la propia página (como
/// el listado de turnos) igual pueden atrapar la ApiException por su cuenta.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempData;

    public ApiExceptionFilter(ITempDataDictionaryFactory tempData) => _tempData = tempData;

    public void OnException(ExceptionContext contexto)
    {
        if (contexto.Exception is not ApiException error) return;

        if (error.Status == StatusCodes.Status401Unauthorized)
        {
            // El ApiClient ya cerró la sesión. Volvemos al login recordando
            // a dónde quería ir el usuario.
            var destino = contexto.HttpContext.Request.Path + contexto.HttpContext.Request.QueryString;
            contexto.Result = new RedirectToActionResult(
                "Login", "Cuenta", new { returnUrl = destino });
        }
        else
        {
            var datos = _tempData.GetTempData(contexto.HttpContext);
            datos["Error"] = error.Message;
            contexto.Result = new RedirectToActionResult("Index", "Home", null);
        }

        contexto.ExceptionHandled = true;
    }
}
