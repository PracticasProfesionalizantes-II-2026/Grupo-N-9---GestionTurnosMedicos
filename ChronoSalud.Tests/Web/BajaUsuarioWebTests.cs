using System.Text.Json;
using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de la Web para la baja y la reactivación: la pestaña "Dadas de
/// baja" del buscador, la confirmación de la baja y el campo Activo.
/// </summary>
public class BajaUsuarioWebTests
{
    [Fact]
    public void El_paginador_de_las_bajas_se_queda_en_las_bajas()
    {
        var listado = new UsuariosIndexViewModel { Buscar = "gomez", VerBajas = true, Pagina = 1 };

        var ruta = listado.Ruta(2);

        Assert.Equal("bajas", ruta["estado"]);
        Assert.Equal("gomez", ruta["buscar"]);
        Assert.Equal("2", ruta["pagina"]);
    }

    [Fact]
    public void Las_pestanas_conservan_la_busqueda_y_vuelven_a_la_primera_pagina()
    {
        var listado = new UsuariosIndexViewModel { Buscar = "gomez", Rol = "doctor", Pagina = 3 };

        var bajas = listado.RutaPestana(true);
        var activas = listado.RutaPestana(false);

        Assert.Equal("bajas", bajas["estado"]);
        Assert.Equal("gomez", bajas["buscar"]);
        Assert.Equal("doctor", bajas["rol"]);
        Assert.False(bajas.ContainsKey("pagina"));
        Assert.False(activas.ContainsKey("estado"));
    }

    [Fact]
    public void Con_turnos_en_pie_no_se_ofrece_la_baja()
    {
        var turno = new TurnoLista(7, DateTime.Today.AddDays(1), "10:00", "confirmado", "Laura Gómez", "clínica", "Ana Duarte");

        var conTurnos = new CambioDeEstadoCuentaViewModel { TurnosQueFrenan = new[] { turno }, TotalQueFrenan = 1 };
        var sinTurnos = new CambioDeEstadoCuentaViewModel();

        Assert.False(conTurnos.PuedeDarDeBaja);
        Assert.True(sinTurnos.PuedeDarDeBaja);
    }

    [Fact]
    public void Si_la_API_no_manda_activo_la_cuenta_se_toma_como_activa()
    {
        // Una API anterior a este paso: el usuario viene sin "activo".
        var json = """{ "idUsuario": 2, "nombre": "Ana", "apellido": "Duarte", "email": "ana@x.com", "telefono": null, "rol": "paciente" }""";

        var usuario = JsonSerializer.Deserialize<UsuarioDetalle>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(usuario);
        Assert.True(usuario.Activo);
    }

    [Fact]
    public void Una_cuenta_dada_de_baja_llega_como_inactiva()
    {
        var json = """{ "idUsuario": 2, "nombre": "Ana", "apellido": "Duarte", "email": "ana@x.com", "telefono": null, "rol": "paciente", "activo": false }""";

        var usuario = JsonSerializer.Deserialize<UsuarioDetalle>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(usuario);
        Assert.False(usuario.Activo);
    }
}
