using System.Text.Json;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// La respuesta de GET /turnos la arma la API como un objeto anónimo
/// { total, turnos, conteos } y la Web la lee con TurnosPagina. Esta prueba
/// hace el mismo viaje que el pedido real.
/// </summary>
public class ContratoListadoTurnosTests
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    [Fact]
    public void La_Web_lee_los_conteos_que_manda_la_API()
    {
        // Preparar: la respuesta tal como la arma TurnoEndpoints.
        var turnos = new List<TurnoListaDto>
        {
            new TurnoListaDto(7, new DateTime(2026, 10, 9), "10:30", "pendiente", "Laura Gómez", "clínica", "Ana Duarte")
        };
        var conteos = new Dictionary<string, int> { ["pendiente"] = 1, ["confirmado"] = 4 };
        var json = JsonSerializer.Serialize(new { total = 1, turnos, conteos }, Opciones);

        // Ejecutar
        var pagina = JsonSerializer.Deserialize<TurnosPagina>(json, Opciones);

        // Verificar
        Assert.NotNull(pagina);
        Assert.Equal(1, pagina.Total);
        Assert.Equal("10:30", pagina.Turnos[0].HoraInicio);
        Assert.NotNull(pagina.Conteos);
        Assert.Equal(4, pagina.Conteos["confirmado"]);
    }

    [Fact]
    public void Si_la_API_todavia_no_manda_conteos_quedan_en_null()
    {
        // La API anterior a este cambio responde solo { total, turnos }.
        var json = JsonSerializer.Serialize(new { total = 0, turnos = new List<TurnoListaDto>() }, Opciones);

        var pagina = JsonSerializer.Deserialize<TurnosPagina>(json, Opciones);

        Assert.NotNull(pagina);
        Assert.Null(pagina.Conteos);
    }
}
