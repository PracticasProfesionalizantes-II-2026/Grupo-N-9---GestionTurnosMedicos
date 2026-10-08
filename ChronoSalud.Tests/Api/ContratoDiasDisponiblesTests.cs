using System.Text.Json;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// GET /doctores/{id}/dias-disponibles: la API arma DiaDisponibleDto y la Web
/// lo lee con DiaDisponible. Esta prueba hace el mismo viaje que el pedido real.
/// </summary>
public class ContratoDiasDisponiblesTests
{
    [Fact]
    public void La_Web_lee_los_dias_que_manda_la_API()
    {
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(new List<DiaDisponibleDto>
        {
            new DiaDisponibleDto(new DateTime(2026, 10, 9), 3)
        }, opciones);

        var dias = JsonSerializer.Deserialize<List<DiaDisponible>>(json, opciones);

        Assert.NotNull(dias);
        Assert.Equal(new DateTime(2026, 10, 9), dias[0].Fecha);
        Assert.Equal(3, dias[0].Libres);
    }
}
