using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace ChronoSaludWeb.Services;

/// <summary>
/// Error devuelto por la API (o de conexión, con Status 0).
/// El mensaje ya viene listo para mostrarle al usuario.
/// </summary>
public class ApiException : Exception
{
    public int Status { get; }

    public ApiException(string mensaje, int status) : base(mensaje) => Status = status;
}

/// <summary>
/// Cliente HTTP tipado contra la API de ChronoSalud. Es el equivalente en C#
/// de chronosalud-front/js/api.js: adjunta el Bearer si hay sesión, traduce los
/// errores { "error": "..." } y corta la sesión cuando la API responde 401.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _contexto;

    // JsonSerializerDefaults.Web = camelCase al enviar, case-insensitive al leer,
    // que es justo como habla la Minimal API.
    private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient http, IHttpContextAccessor contexto)
    {
        _http = http;
        _contexto = contexto;
    }

    public Task<T?> GetAsync<T>(string ruta, IDictionary<string, object?>? parametros = null, bool anonimo = false)
        => PedirAsync<T>(HttpMethod.Get, ruta, null, parametros, anonimo);

    public Task<T?> PostAsync<T>(string ruta, object? cuerpo, bool anonimo = false)
        => PedirAsync<T>(HttpMethod.Post, ruta, cuerpo, null, anonimo);

    public Task PostAsync(string ruta, object? cuerpo, bool anonimo = false)
        => PedirAsync<object>(HttpMethod.Post, ruta, cuerpo, null, anonimo);

    public Task<T?> PutAsync<T>(string ruta, object? cuerpo)
        => PedirAsync<T>(HttpMethod.Put, ruta, cuerpo, null, false);

    public Task PutAsync(string ruta, object? cuerpo)
        => PedirAsync<object>(HttpMethod.Put, ruta, cuerpo, null, false);

    public Task<T?> PatchAsync<T>(string ruta, object? cuerpo)
        => PedirAsync<T>(HttpMethod.Patch, ruta, cuerpo, null, false);

    public Task PatchAsync(string ruta, object? cuerpo)
        => PedirAsync<object>(HttpMethod.Patch, ruta, cuerpo, null, false);

    public Task DeleteAsync(string ruta)
        => PedirAsync<object>(HttpMethod.Delete, ruta, null, null, false);

    private async Task<T?> PedirAsync<T>(
        HttpMethod metodo,
        string ruta,
        object? cuerpo,
        IDictionary<string, object?>? parametros,
        bool anonimo)
    {
        using var pedido = new HttpRequestMessage(metodo, ConstruirUrl(ruta, parametros));

        if (cuerpo is not null)
            pedido.Content = JsonContent.Create(cuerpo, options: JsonOpciones);

        if (!anonimo)
        {
            var token = _contexto.HttpContext?.Session.ObtenerSesion()?.Token;
            if (!string.IsNullOrEmpty(token))
                pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await _http.SendAsync(pedido);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(
                $"No se pudo conectar con la API. ¿Está levantada en {_http.BaseAddress}?", 0);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("La API tardó demasiado en responder.", 0);
        }

        using (respuesta)
        {
            var estado = (int)respuesta.StatusCode;

            // El token venció o no es válido: se cierra la sesión y el controlador
            // decide a dónde mandar al usuario. En los pedidos anónimos (el login)
            // un 401 significa "credenciales incorrectas", no "sesión vencida".
            if (estado == StatusCodes.Status401Unauthorized && !anonimo)
            {
                _contexto.HttpContext?.Session.CerrarSesion();
                throw new ApiException("Sesión expirada. Volvé a iniciar sesión.", 401);
            }

            if (estado == StatusCodes.Status204NoContent)
                return default;

            var texto = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
                throw new ApiException(LeerError(texto) ?? $"Error {estado}", estado);

            if (string.IsNullOrWhiteSpace(texto))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(texto, JsonOpciones);
            }
            catch (JsonException)
            {
                throw new ApiException("La API devolvió una respuesta que no se pudo interpretar.", estado);
            }
        }
    }

    private string ConstruirUrl(string ruta, IDictionary<string, object?>? parametros)
    {
        if (parametros is null || parametros.Count == 0)
            return ruta;

        // Igual que en api.js: los parámetros vacíos no se mandan.
        var query = new Dictionary<string, string?>();
        foreach (var (clave, valor) in parametros)
        {
            var texto = Formatear(valor);
            if (!string.IsNullOrEmpty(texto))
                query[clave] = texto;
        }

        return query.Count == 0 ? ruta : QueryHelpers.AddQueryString(ruta, query);
    }

    private static string? Formatear(object? valor) => valor switch
    {
        null            => null,
        string s        => s,
        bool b          => b ? "true" : "false",
        DateTime f      => f.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
        DateOnly f      => f.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable f  => f.ToString(null, CultureInfo.InvariantCulture),
        _               => valor.ToString()
    };

    /// <summary>
    /// La API devuelve los errores como { "error": "..." }. Si vino otra cosa
    /// (un ProblemDetails, HTML, o nada) devolvemos null y el que llama arma
    /// un mensaje genérico con el código de estado.
    /// </summary>
    private static string? LeerError(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(texto);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String)
                return error.GetString();

            // Los errores de validación del framework vienen como ProblemDetails.
            if (doc.RootElement.TryGetProperty("title", out var titulo) &&
                titulo.ValueKind == JsonValueKind.String)
                return titulo.GetString();

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
