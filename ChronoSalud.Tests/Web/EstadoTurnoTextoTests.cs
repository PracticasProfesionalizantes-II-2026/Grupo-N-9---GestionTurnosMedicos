using ChronoSaludWeb.Models;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Al paciente el estado del turno se le dice con palabras; el personal sigue
/// viendo la palabra corta que usa en las listas.
/// </summary>
public class EstadoTurnoTextoTests
{
    [Theory]
    [InlineData("pendiente", "Esperando confirmación")]
    [InlineData("completado", "Atendido")]
    [InlineData("confirmado", "Confirmado")]
    [InlineData("cancelado", "Cancelado")]
    [InlineData("ausente", "Ausente")]
    [InlineData("PENDIENTE", "Esperando confirmación")]
    public void Al_paciente_se_le_dice_con_palabras(string estado, string esperado)
    {
        Assert.Equal(esperado, EstadoTurnoTexto.Para(estado, paciente: true));
    }

    [Theory]
    [InlineData("pendiente", "Pendiente")]
    [InlineData("completado", "Completado")]
    [InlineData("confirmado", "Confirmado")]
    public void El_personal_ve_la_palabra_corta(string estado, string esperado)
    {
        Assert.Equal(esperado, EstadoTurnoTexto.Para(estado, paciente: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_estado_no_rompe(string? estado)
    {
        Assert.Equal("Sin estado", EstadoTurnoTexto.Para(estado, paciente: true));
    }

    [Fact]
    public void Un_estado_desconocido_se_muestra_tal_cual()
    {
        Assert.Equal("Reprogramado", EstadoTurnoTexto.Para("reprogramado", paciente: true));
    }
}
