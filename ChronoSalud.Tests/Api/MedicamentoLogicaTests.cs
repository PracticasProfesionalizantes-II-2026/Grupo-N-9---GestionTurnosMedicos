using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de DELETE /medicamentos/{id}: un medicamento que figura en una
/// receta no se borra (paso 10).
/// </summary>
public class MedicamentoLogicaTests
{
    private readonly MedicamentoRepositoryFalso _medicamentos = new MedicamentoRepositoryFalso();
    private readonly MedicamentoLogica _logica;

    public MedicamentoLogicaTests()
    {
        _logica = new MedicamentoLogica(_medicamentos);
        _medicamentos.Medicamentos.Add(new Medicamento { Id = 1, Nombre = "IBUPIRAC" });
    }

    [Fact]
    public async Task Borrar_uno_que_no_existe_da_no_encontrado()
    {
        var (ok, error) = await _logica.Eliminar(99);

        Assert.False(ok);
        Assert.Contains("no encontrado", error);
    }

    [Fact]
    public async Task No_borra_un_medicamento_que_esta_en_una_receta()
    {
        _medicamentos.Recetados.Add(1);

        var (ok, error) = await _logica.Eliminar(1);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.DoesNotContain("no encontrado", error);
        Assert.Single(_medicamentos.Medicamentos);
    }

    [Fact]
    public async Task Borra_un_medicamento_que_no_esta_en_ninguna_receta()
    {
        var (ok, error) = await _logica.Eliminar(1);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Empty(_medicamentos.Medicamentos);
    }
}
