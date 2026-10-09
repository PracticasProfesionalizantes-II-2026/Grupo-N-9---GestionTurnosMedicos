using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de quién escribió cada entrada de la historia clínica (paso 10).
/// </summary>
public class HistorialClinicoLogicaTests
{
    [Fact]
    public async Task La_entrada_dice_quien_la_escribio()
    {
        // Preparar: una entrada escrita por la doctora Méndez, con su cuenta
        // cargada como la trae EF con el Include.
        var entradas = new HistorialClinicoRepositoryFalso();
        var doctora = new Doctor
        {
            Id = 20,
            IdUsuario = 3,
            Especialidad = "Clínica médica",
            Matricula = "MN 1234",
            Usuario = new Usuario { Id = 3, Nombre = "Laura", Apellido = "Méndez", Rol = "doctor" }
        };
        entradas.Entradas.Add(new HistorialClinico
        {
            Id = 1,
            IdPaciente = 10,
            IdDoctor = 20,
            Doctor = doctora,
            Descripcion = "Control anual.",
            Diagnostico = "Sano"
        });

        var logica = new HistorialClinicoLogica(
            entradas, new DoctorRepositoryFalso(), new PacienteRepositoryFalso(), new TurnoRepositoryFalso());

        // Ejecutar: la mira un doctor (personal).
        var (_, historiales, error, _) = await logica.ObtenerDePaciente(10, null, null, 3, callerEsStaff: true);

        // Verificar
        Assert.Null(error);
        var entrada = Assert.Single(historiales);
        Assert.NotNull(entrada.Doctor);
        Assert.Equal("Laura", entrada.Doctor.Nombre);
        Assert.Equal("Méndez", entrada.Doctor.Apellido);
        Assert.Equal("MN 1234", entrada.Doctor.Matricula);
    }
}
