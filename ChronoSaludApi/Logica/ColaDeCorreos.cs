using System.Threading.Channels;

namespace ChronoSaludApi.Logica;

/// <summary>
/// El <see cref="IEnviadorDeCorreo"/> que usa la lógica: no manda nada, deja el
/// correo en una cola en memoria y vuelve enseguida. Lo manda
/// <see cref="EnvioDeCorreos"/>, en segundo plano.
///
/// Así un pedido nunca espera al servidor de correo (que puede tardar o
/// fallar), y "Olvidé mi contraseña" responde en el mismo tiempo exista o no
/// la cuenta. Si la API se reinicia con correos en la cola, esos se pierden:
/// son avisos, no datos.
/// </summary>
public class ColaDeCorreos : IEnviadorDeCorreo
{
    private readonly Channel<CorreoSaliente> _canal = Channel.CreateUnbounded<CorreoSaliente>();

    public ChannelReader<CorreoSaliente> Pendientes => _canal.Reader;

    public Task Enviar(string para, string asunto, string texto)
    {
        // Un usuario sin email no tiene a dónde recibirlo.
        if (string.IsNullOrWhiteSpace(para))
            return Task.CompletedTask;

        _canal.Writer.TryWrite(new CorreoSaliente(para.Trim(), asunto, texto));
        return Task.CompletedTask;
    }
}
