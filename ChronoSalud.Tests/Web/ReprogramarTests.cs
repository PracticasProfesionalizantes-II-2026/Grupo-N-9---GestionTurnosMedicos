using ChronoSaludWeb.Models;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de reprogramar: los enlaces de la tira y de los horarios, y el
/// resumen "de … a …".
/// </summary>
public class ReprogramarTests
{
    // El 8 de octubre de 2026 es jueves.
    private static readonly DateTime Jueves = new DateTime(2026, 10, 8);

    [Fact]
    public void La_tira_conserva_la_ruta_base_y_suma_el_dia()
    {
        // Preparar: la ruta base de reprogramar es el id del turno.
        var rutaBase = new Dictionary<string, string> { ["id"] = "12" };

        // Ejecutar
        var ruta = TiraDeDiasViewModel.RutaConFechas(rutaBase, Jueves, Jueves.AddDays(-3));

        // Verificar: no toca la base y suma el día y el comienzo de la tira.
        Assert.Equal("12", ruta["id"]);
        Assert.Equal("2026-10-08", ruta["fecha"]);
        Assert.Equal("2026-10-05", ruta["desde"]);
        Assert.Single(rutaBase);
    }

    [Fact]
    public void La_tira_sabe_cual_es_el_dia_elegido()
    {
        var tira = new TiraDeDiasViewModel
        {
            Dias = new[]
            {
                new DiaDeLaTira { Fecha = Jueves, Libres = 0 },
                new DiaDeLaTira { Fecha = Jueves.AddDays(1), Libres = 2, Elegido = true }
            }
        };

        Assert.Equal(Jueves.AddDays(1), tira.DiaElegido);
        Assert.False(tira.SinLugarEnNingunDia);
    }

    [Fact]
    public void Cada_horario_lleva_a_confirmar_con_el_turno_el_dia_y_las_horas()
    {
        var modelo = new TurnoReprogramarViewModel
        {
            Turno = new TurnoDetalleViewModel { IdTurno = 12 },
            Fecha = Jueves
        };

        var ruta = modelo.RutaConfirmar(new FranjaViewModel { IdDoctor = 4, HoraInicio = "11:00", HoraFin = "11:30" });

        Assert.Equal("12", ruta["id"]);
        Assert.Equal("2026-10-08", ruta["fecha"]);
        Assert.Equal("11:00", ruta["inicio"]);
        Assert.Equal("11:30", ruta["fin"]);
    }

    [Fact]
    public void El_resumen_dice_de_donde_a_donde_se_mueve()
    {
        var modelo = new ConfirmarReprogramacionViewModel
        {
            Turno = new TurnoDetalleViewModel { IdTurno = 12, FechaInicio = Jueves, HoraInicio = "10:00", HoraFin = "10:30" },
            Fecha = Jueves.AddDays(4),
            HoraInicio = "11:00",
            HoraFin = "11:30"
        };

        Assert.Equal("jueves 8 de octubre", modelo.FechaActualLarga);
        Assert.Equal("lunes 12 de octubre", modelo.FechaNuevaLarga);
        Assert.Equal("11:00 a 11:30", modelo.HorarioNuevo);
        Assert.Equal("2026-10-12", modelo.RutaElegirOtro["fecha"]);
    }
}
