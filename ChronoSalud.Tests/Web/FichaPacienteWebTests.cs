using ChronoSaludWeb.Models;
using ChronoSaludWeb.Models.ViewModels;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de cómo la Web arma la ficha que manda a la API, y de cómo la
/// muestra en Pacientes/Detalle.
/// </summary>
public class FichaPacienteWebTests
{
    [Fact]
    public void El_alta_manda_todos_los_datos_del_formulario()
    {
        // Preparar
        var formulario = new PacienteCreateViewModel
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "  30111222 ",
            FechaNacimiento = new DateOnly(1990, 5, 20),
            Sexo = "femenino",
            Provincia = "Córdoba",
            Localidad = "Villa María",
            CodigoPostal = "",
            ContactoEmergenciaNombre = "Marta Pérez"
        };

        // Ejecutar
        var ficha = formulario.ArmarFicha();

        // Verificar: el número de documento va como DNI y sin espacios, y lo
        // vacío va como null (no se manda).
        Assert.Equal("30111222", ficha.Dni);
        Assert.Equal("DNI", ficha.TipoDocumento);
        Assert.Equal(new DateTime(1990, 5, 20), ficha.FechaNacimiento);
        Assert.Equal("femenino", ficha.Sexo);
        Assert.Equal("Villa María", ficha.Localidad);
        Assert.Equal("Marta Pérez", ficha.ContactoEmergenciaNombre);
        Assert.Null(ficha.CodigoPostal);
        Assert.Null(ficha.Borrar);
    }

    [Fact]
    public void La_edicion_pide_borrar_los_campos_que_quedaron_vacios()
    {
        // Preparar: el administrador vació las alergias y dejó la dirección.
        var formulario = new PacienteEditarViewModel
        {
            Dni = "30111222",
            Direccion = "San Martín 100",
            Alergias = "   "
        };

        // Ejecutar
        var ficha = formulario.ArmarFicha();

        // Verificar: lo que tiene valor se manda y lo vacío va en Borrar.
        Assert.Equal("San Martín 100", ficha.Direccion);
        Assert.NotNull(ficha.Borrar);
        Assert.Contains("alergias", ficha.Borrar);
        Assert.DoesNotContain("direccion", ficha.Borrar);
        Assert.DoesNotContain("dni", ficha.Borrar);
    }

    [Fact]
    public void La_direccion_completa_saltea_lo_que_falta()
    {
        var ficha = new PacienteDetalleViewModel
        {
            Direccion = "San Martín 100",
            Provincia = "Córdoba",
            CodigoPostal = "5900"
        };

        Assert.Equal("San Martín 100, Córdoba, CP 5900", ficha.DireccionCompleta);
    }

    [Fact]
    public void Sin_numero_de_documento_no_se_muestra_documento()
    {
        var ficha = new PacienteDetalleViewModel { TipoDocumento = "DNI" };

        Assert.Null(ficha.Documento);
    }
}
