using ChronoSaludWeb.Models;
using ChronoSaludWeb.Services;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de cómo la Web muestra una receta con la copia del medicamento y
/// quién la firmó (paso 10).
/// </summary>
public class RecetasWebTests
{
    private static Receta RecetaCon(params MedicamentoRecetado[] medicamentos) => new Receta(
        12,
        new DateTime(2026, 10, 9),
        new DateTime(2026, 11, 8),
        null,
        medicamentos,
        new Profesional(20, "Laura", "Méndez", "Clínica médica", "MN 1234"));

    [Fact]
    public void El_nombre_sale_de_la_copia_que_guardo_la_receta()
    {
        var receta = RecetaCon(new MedicamentoRecetado(
            1, "IBUPIRAC", "IBUPROFENO", "400 mg", "COMPRIMIDO", "1 comprimido", "cada 8 horas", null, null));

        var fila = RecetaFilaViewModel.Desde(receta);

        var medicamento = Assert.Single(fila.Medicamentos);
        Assert.False(medicamento.EsOtro);
        Assert.Equal("IBUPIRAC", medicamento.NombreMostrado);
        Assert.Equal("IBUPIRAC (ibuprofeno) 400 mg · comprimido", medicamento.DescripcionCompleta);
        Assert.Equal("Ibuprofeno 400 mg · comprimido (IBUPIRAC)", medicamento.DescripcionParaImprimir);
    }

    [Fact]
    public void Una_fila_Otro_muestra_el_nombre_escrito_a_mano()
    {
        var receta = RecetaCon(new MedicamentoRecetado(
            5, MedicamentoService.NombreMarcadorOtro, null, null, null, "1 sobre", "cada 12 horas", null,
            IndicacionesDeOtro.Componer("Sales de rehidratación", "Disolver en agua.")));

        var medicamento = Assert.Single(RecetaFilaViewModel.Desde(receta).Medicamentos);

        Assert.True(medicamento.EsOtro);
        Assert.Equal("Sales de rehidratación", medicamento.NombreMostrado);
        Assert.Equal("Sales de rehidratación", medicamento.DescripcionParaImprimir);
        Assert.Equal("Disolver en agua.", medicamento.IndicacionesMostradas);
    }

    [Fact]
    public void La_receta_muestra_quien_la_firmo()
    {
        var fila = RecetaFilaViewModel.Desde(RecetaCon());

        Assert.NotNull(fila.Firma);
        Assert.Equal("Laura Méndez", fila.Firma.NombreCompleto);
        Assert.Equal("Laura Méndez · Clínica médica · Matrícula MN 1234", fila.Firma.Linea);
    }

    [Fact]
    public void La_firma_omite_lo_que_falta()
    {
        var firma = FirmaViewModel.Desde(new Profesional(20, "Laura", "Méndez", "", " "));

        Assert.NotNull(firma);
        Assert.Null(firma.Detalle);
        Assert.Null(firma.MatriculaMostrada);
        Assert.Equal("Laura Méndez", firma.Linea);
    }

    [Fact]
    public void Sin_doctor_no_hay_firma()
    {
        Assert.Null(FirmaViewModel.Desde(null));
    }

    [Fact]
    public void La_hoja_muestra_el_documento_del_paciente()
    {
        Assert.Equal("DNI 30111222", new RecetaImprimirViewModel { TipoDocumento = "DNI", Dni = "30111222" }.Documento);
        Assert.Equal("30111222", new RecetaImprimirViewModel { Dni = "30111222" }.Documento);
        Assert.Null(new RecetaImprimirViewModel { TipoDocumento = "DNI" }.Documento);
    }
}
