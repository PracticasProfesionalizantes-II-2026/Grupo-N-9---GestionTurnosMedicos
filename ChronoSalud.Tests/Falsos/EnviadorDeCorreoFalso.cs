using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Falsos;

/// <summary>
/// Reemplaza al enviador de emails: en vez de mandarlos, los guarda en una
/// lista para que la prueba revise a quién se mandó y qué decía.
/// </summary>
public class EnviadorDeCorreoFalso : IEnviadorDeCorreo
{
    public List<(string Para, string Asunto, string Texto)> Enviados { get; } = new();

    public Task Enviar(string para, string asunto, string texto)
    {
        Enviados.Add((para, asunto, texto));
        return Task.CompletedTask;
    }
}
