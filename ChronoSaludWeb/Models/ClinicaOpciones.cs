namespace ChronoSaludWeb.Models;

/// <summary>
/// Datos de contacto de la clínica, de la sección "Clinica" de appsettings.json.
/// Hoy están vacíos a propósito: los botones de "¿Necesitás ayuda?" se ven
/// pero no hacen nada, y el comprobante del turno no muestra dirección. Para
/// activarlos alcanza con cargar los valores (en Azure, por ejemplo
/// Clinica__Telefono), sin tocar código.
/// </summary>
public class ClinicaOpciones
{
    /// <summary>Dirección para el comprobante y el calendario. Ej.: "Av. Siempreviva 742, CABA".</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Horario de atención. Ej.: "de lunes a viernes de 8 a 18".</summary>
    public string Horario { get; set; } = string.Empty;

    /// <summary>Teléfono para llamar, tal como se marca. Ej.: "1140000000".</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Número de WhatsApp con código de país y sin signos. Ej.: "5491140000000".</summary>
    public string WhatsApp { get; set; } = string.Empty;

    public bool TieneTelefono => !string.IsNullOrWhiteSpace(Telefono);

    public bool TieneWhatsApp => !string.IsNullOrWhiteSpace(WhatsApp);

    public bool TieneDireccion => !string.IsNullOrWhiteSpace(Direccion);

    public bool TieneHorario => !string.IsNullOrWhiteSpace(Horario);
}
