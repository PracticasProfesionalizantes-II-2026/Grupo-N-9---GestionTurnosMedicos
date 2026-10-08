using ChronoSaludWeb.Models;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas del listado de turnos de la Web: los enlaces que arma (página,
/// "ver todos") y lo que muestra cuando la API no manda los conteos.
/// </summary>
public class ListadoTurnosTests
{
    [Fact]
    public void El_enlace_a_otra_pagina_conserva_los_filtros()
    {
        // Preparar
        var filtros = new TurnosFiltroViewModel
        {
            Estado = "pendiente",
            VerTodos = true,
            Orden = "paciente",
            Descendente = true,
            Pagina = 2
        };

        // Ejecutar
        var ruta = filtros.RutaDePagina(3);

        // Verificar
        Assert.Equal("pendiente", ruta["estado"]);
        Assert.Equal("todos", ruta["ver"]);
        Assert.Equal("paciente", ruta["orden"]);
        Assert.Equal("desc", ruta["dir"]);
        Assert.Equal("3", ruta["pagina"]);
    }

    [Fact]
    public void Pasar_a_proximos_conserva_el_estado_y_vuelve_al_orden_de_siempre()
    {
        var filtros = new TurnosFiltroViewModel
        {
            Estado = "confirmado",
            VerTodos = true,
            Orden = "fecha",
            Descendente = true,
            Pagina = 4
        };

        var ruta = filtros.RutaVerTodos(false);

        Assert.Equal("confirmado", ruta["estado"]);
        Assert.False(ruta.ContainsKey("ver"));
        Assert.False(ruta.ContainsKey("orden"));
        Assert.False(ruta.ContainsKey("dir"));
        Assert.False(ruta.ContainsKey("pagina"));
    }

    [Theory]
    [InlineData("hora", "fecha")]
    [InlineData("fecha", "fecha")]
    [InlineData("paciente", "paciente")]
    [InlineData("estado", "estado")]
    [InlineData(null, "fecha")]
    public void La_columna_hora_se_le_pide_a_la_api_como_fecha(string? columna, string esperado)
    {
        var filtros = new TurnosFiltroViewModel { Orden = columna };

        Assert.Equal(esperado, filtros.OrdenParaApi);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(45, 3)]
    public void Cuenta_las_paginas_de_a_20(int total, int esperadas)
    {
        Assert.Equal(esperadas, PaginadorViewModel.ContarPaginas(total, 20));
    }

    [Fact]
    public void Sin_conteos_de_la_api_las_tarjetas_muestran_un_guion()
    {
        // Durante un despliegue la Web nueva puede hablar unos minutos con la API vieja.
        var sinConteos = new TurnosIndexViewModel();
        var conConteos = new TurnosIndexViewModel
        {
            Conteos = new Dictionary<string, int> { ["pendiente"] = 3 }
        };

        Assert.Equal("—", sinConteos.Pendientes);
        Assert.Equal("3", conConteos.Pendientes);
        Assert.Equal("0", conConteos.Confirmados);
    }
}
