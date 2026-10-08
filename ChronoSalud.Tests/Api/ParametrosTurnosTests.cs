using ChronoSaludApi.Endpoints;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de cómo GET /turnos lee "estado", "estados" y "orden" de la URL.
/// </summary>
public class ParametrosTurnosTests
{
    [Fact]
    public void Junta_estado_y_estados_sin_repetir_ni_vacios()
    {
        // Ejecutar: mayúsculas, espacios, un repetido y un vacío.
        var estados = TurnoEndpoints.LeerEstados("Pendiente", "confirmado, pendiente,,cancelado");

        // Verificar
        Assert.Equal(new List<string> { "pendiente", "confirmado", "cancelado" }, estados);
    }

    [Fact]
    public void Sin_estados_la_lista_queda_vacia()
    {
        var estados = TurnoEndpoints.LeerEstados(null, "  ");

        Assert.Empty(estados);
    }

    [Theory]
    [InlineData("Paciente", "paciente")]
    [InlineData("estado", "estado")]
    [InlineData("fecha", "fecha")]
    [InlineData("cualquiera", "fecha")]
    [InlineData(null, "fecha")]
    public void Un_orden_desconocido_ordena_por_fecha(string? pedido, string esperado)
    {
        Assert.Equal(esperado, TurnoEndpoints.LeerOrden(pedido));
    }
}
