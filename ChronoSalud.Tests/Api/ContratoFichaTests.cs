using System.Text.Json;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// La Web arma la ficha con su clase (DatosFichaPaciente) y la API la lee con
/// la suya (PacienteUpdateDto). Esta prueba hace el mismo viaje que el pedido
/// real: la Web la convierte a JSON y la API lo lee.
/// </summary>
public class ContratoFichaTests
{
    [Fact]
    public void La_API_entiende_la_ficha_que_manda_la_Web()
    {
        // Preparar: la ficha como la arma la Web.
        var fichaWeb = new DatosFichaPaciente
        {
            Dni = "30111222",
            Localidad = "Villa María",
            ContactoEmergenciaTelefono = "353-4000000",
            Borrar = new List<string> { "alergias" }
        };

        // Ejecutar: las dos aplicaciones usan las opciones "Web" de JSON
        // (nombres en camelCase y sin distinguir mayúsculas al leer).
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(fichaWeb, opciones);
        var fichaApi = JsonSerializer.Deserialize<PacienteUpdateDto>(json, opciones);

        // Verificar
        Assert.NotNull(fichaApi);
        Assert.Equal("30111222", fichaApi.Dni);
        Assert.Equal("Villa María", fichaApi.Localidad);
        Assert.Equal("353-4000000", fichaApi.ContactoEmergenciaTelefono);
        Assert.Equal(new List<string> { "alergias" }, fichaApi.Borrar);
    }
}
