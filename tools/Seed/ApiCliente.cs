using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChronoSalud.Seed;

/// <summary>
/// Resultado de una llamada a la API. Nunca se lanza una excepcion hacia afuera:
/// el seeder necesita distinguir un 409 esperable ("ya existe") de una falla real,
/// y con excepciones eso queda mucho mas engorroso.
/// </summary>
internal sealed record RespuestaApi(bool Ok, int Estado, JsonElement Datos, string? Error);

/// <summary>
/// Envoltorio minimo sobre HttpClient para hablar con la API de ChronoSalud.
/// </summary>
internal sealed class ApiCliente : IDisposable
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        // La API espera camelCase (idPaciente, fechaInicio, ...), asi que las
        // propiedades se escriben en PascalCase y se convierten al serializar.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public ApiCliente(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public Task<RespuestaApi> GetAsync(string ruta, string? token = null)
        => EnviarAsync(HttpMethod.Get, ruta, null, token);

    public Task<RespuestaApi> PostAsync(string ruta, object cuerpo, string? token = null)
        => EnviarAsync(HttpMethod.Post, ruta, cuerpo, token);

    public Task<RespuestaApi> PutAsync(string ruta, object cuerpo, string? token = null)
        => EnviarAsync(HttpMethod.Put, ruta, cuerpo, token);

    private async Task<RespuestaApi> EnviarAsync(HttpMethod metodo, string ruta, object? cuerpo, string? token)
    {
        using var pedido = new HttpRequestMessage(metodo, _baseUrl + ruta);

        if (!string.IsNullOrEmpty(token))
        {
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (cuerpo is not null)
        {
            var json = JsonSerializer.Serialize(cuerpo, OpcionesJson);
            pedido.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await _http.SendAsync(pedido);
        }
        catch (Exception ex)
        {
            // Estado 0 = ni siquiera se pudo contactar la API.
            return new RespuestaApi(false, 0, default, ex.Message);
        }

        using (respuesta)
        {
            var estado = (int)respuesta.StatusCode;
            var texto = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                return new RespuestaApi(false, estado, default, ExtraerError(texto, respuesta.ReasonPhrase));
            }

            var datos = default(JsonElement);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                try
                {
                    // Clone() desprende el elemento del JsonDocument, que se libera aca.
                    using var documento = JsonDocument.Parse(texto);
                    datos = documento.RootElement.Clone();
                }
                catch (JsonException)
                {
                    // Hay endpoints que responden 204 o texto plano: no es un error.
                }
            }

            return new RespuestaApi(true, estado, datos, null);
        }
    }

    /// <summary>
    /// La API devuelve los errores como { "error": "..." }. Si el cuerpo no es
    /// JSON valido se usa el texto crudo, y si viene vacio el motivo del HTTP.
    /// </summary>
    private static string ExtraerError(string texto, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return motivo ?? "sin detalle";
        }

        try
        {
            using var documento = JsonDocument.Parse(texto);
            var mensaje = Json.Texto(documento.RootElement, "error", "message", "title");
            return string.IsNullOrWhiteSpace(mensaje) ? texto : mensaje;
        }
        catch (JsonException)
        {
            return texto;
        }
    }

    public void Dispose() => _http.Dispose();
}

/// <summary>
/// Lectura tolerante de JSON. PowerShell accede a las propiedades sin distinguir
/// mayusculas y minusculas; JsonElement si distingue, y ademas la API mezcla
/// estilos (idTurno en el listado, id_turno al crear). Estos helpers buscan
/// entre varios nombres posibles para que el seeder no dependa de eso.
/// </summary>
internal static class Json
{
    public static bool Buscar(JsonElement elemento, out JsonElement valor, params string[] nombres)
    {
        valor = default;

        if (elemento.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var propiedad in elemento.EnumerateObject())
        {
            foreach (var nombre in nombres)
            {
                if (string.Equals(propiedad.Name, nombre, StringComparison.OrdinalIgnoreCase))
                {
                    valor = propiedad.Value;
                    return true;
                }
            }
        }

        return false;
    }

    public static int Entero(JsonElement elemento, params string[] nombres)
    {
        if (!Buscar(elemento, out var valor, nombres))
        {
            return 0;
        }

        return valor.ValueKind switch
        {
            JsonValueKind.Number => valor.TryGetInt32(out var numero) ? numero : 0,
            JsonValueKind.String => int.TryParse(valor.GetString(), out var texto) ? texto : 0,
            _ => 0
        };
    }

    public static string Texto(JsonElement elemento, params string[] nombres)
    {
        if (!Buscar(elemento, out var valor, nombres))
        {
            return string.Empty;
        }

        return valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? string.Empty
            : valor.ToString();
    }
}
