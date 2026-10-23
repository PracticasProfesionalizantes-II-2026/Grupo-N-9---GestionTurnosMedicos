namespace ChronoSaludApi.Logica;

/// <summary>Un email listo para salir.</summary>
public record CorreoSaliente(string Para, string Asunto, string Texto);

/// <summary>
/// Lo que manda un email de verdad (o hace como que lo manda). La lógica no lo
/// usa directo: deja el correo en la cola (<see cref="ColaDeCorreos"/>) y
/// <see cref="EnvioDeCorreos"/> lo pasa por acá en segundo plano.
/// </summary>
public interface ITransporteDeCorreo
{
    Task Mandar(CorreoSaliente correo, CancellationToken cancelacion);
}

/// <summary>Qué transporte se usa según la configuración.</summary>
public enum TipoDeTransporte
{
    /// <summary>Hay servidor SMTP: los correos salen de verdad.</summary>
    Smtp,

    /// <summary>Sin SMTP, en desarrollo (o pidiéndolo): se escriben en el log.</summary>
    Consola,

    /// <summary>Sin SMTP fuera de desarrollo: no salen ni se escriben, solo se avisa.</summary>
    SinConfigurar
}

public static class EleccionDeTransporte
{
    public static TipoDeTransporte Para(OpcionesDeCorreo opciones, bool esDesarrollo)
    {
        if (opciones.Smtp.HayServidor) return TipoDeTransporte.Smtp;
        if (esDesarrollo || opciones.MostrarEnLog) return TipoDeTransporte.Consola;
        return TipoDeTransporte.SinConfigurar;
    }

    /// <summary>
    /// "a***@gmail.com": para el log, que no tiene por qué guardar el email
    /// completo de cada paciente.
    /// </summary>
    public static string Enmascarar(string email)
    {
        var arroba = email.IndexOf('@');
        if (arroba <= 0) return "***";
        return $"{email[0]}***{email[arroba..]}";
    }
}
