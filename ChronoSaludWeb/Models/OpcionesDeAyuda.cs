namespace ChronoSaludWeb.Models;

/// <summary>
/// Una forma de pedir ayuda en la ventanita "¿Necesitás ayuda?" del pie.
/// Sin <see cref="Url"/> se muestra apagada, con "Próximamente".
/// </summary>
public record OpcionAyuda(string Titulo, string Icono, string? Url)
{
    public bool Disponible => Url is not null;
}

/// <summary>
/// Las opciones de la ventanita de ayuda, en el orden en que se muestran.
/// Para sumar una nueva (por ejemplo, un email o un chat) alcanza con
/// agregar una línea en <see cref="Armar"/>.
/// </summary>
public static class OpcionesDeAyuda
{
    public static IReadOnlyList<OpcionAyuda> Armar(ClinicaOpciones clinica) =>
    [
        // wa.me abre la conversación en la app o en WhatsApp Web.
        new("Escribir por WhatsApp", "mensaje", clinica.TieneWhatsApp ? $"https://wa.me/{clinica.WhatsApp.Trim()}" : null),

        // tel: llama con un toque desde el celular.
        new("Llamar a la clínica", "telefono", clinica.TieneTelefono ? $"tel:{clinica.Telefono.Trim()}" : null)
    ];
}
