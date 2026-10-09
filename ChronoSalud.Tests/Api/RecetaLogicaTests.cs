using ChronoSalud.Tests.Falsos;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica;
using ChronoSaludApi.Logica.DTOs;

namespace ChronoSalud.Tests.Api;

/// <summary>
/// Pruebas de la copia del medicamento que guarda cada receta y de quién la
/// firmó (paso 10).
/// </summary>
public class RecetaLogicaTests
{
    // La doctora que emite las recetas (usuario 3, perfil de doctor 20).
    private const int IdUsuarioDoctora = 3;
    private const int IdPaciente = 10;

    private readonly RecetaRepositoryFalso _recetas = new RecetaRepositoryFalso();
    private readonly MedicamentoRepositoryFalso _medicamentos = new MedicamentoRepositoryFalso();
    private readonly PacienteRepositoryFalso _pacientes = new PacienteRepositoryFalso();
    private readonly DoctorRepositoryFalso _doctores = new DoctorRepositoryFalso();
    private readonly RecetaLogica _logica;

    private readonly Medicamento _ibuprofeno;

    public RecetaLogicaTests()
    {
        _logica = new RecetaLogica(_recetas, _medicamentos, _pacientes, _doctores);

        var cuenta = new Usuario { Id = IdUsuarioDoctora, Nombre = "Laura", Apellido = "Méndez", Rol = "doctor" };
        _doctores.Doctores.Add(new Doctor
        {
            Id = 20,
            IdUsuario = IdUsuarioDoctora,
            Especialidad = "Clínica médica",
            Matricula = "MN 1234",
            Usuario = cuenta
        });

        _ibuprofeno = new Medicamento
        {
            Id = 1,
            Nombre = "IBUPIRAC",
            NombreGenerico = "IBUPROFENO",
            Concentracion = "400 mg",
            FormaFarmaceutica = "COMPRIMIDO"
        };
        _medicamentos.Medicamentos.Add(_ibuprofeno);
    }

    private static RecetaCreateDto Pedido(int idMedicamento) => new RecetaCreateDto(
        IdPaciente,
        0,
        null,
        new DateTime(2026, 10, 9),
        new DateTime(2026, 11, 8),
        null,
        new List<RecetaMedicamentoDto>
        {
            new RecetaMedicamentoDto(idMedicamento, "1 comprimido", "cada 8 horas", "5 días", null)
        });

    [Fact]
    public async Task Al_emitir_guarda_una_copia_del_medicamento()
    {
        var (id, error, _) = await _logica.Crear(Pedido(_ibuprofeno.Id), IdUsuarioDoctora, callerEsDoctor: true);

        Assert.Null(error);
        var renglon = _recetas.Recetas.Single(r => r.Id == id).RecetaMedicamentos.Single();
        Assert.Equal("IBUPIRAC", renglon.NombreMedicamento);
        Assert.Equal("IBUPROFENO", renglon.NombreGenerico);
        Assert.Equal("400 mg", renglon.Concentracion);
        Assert.Equal("COMPRIMIDO", renglon.FormaFarmaceutica);
    }

    [Fact]
    public async Task Editar_el_medicamento_despues_no_cambia_la_receta()
    {
        var (id, _, _) = await _logica.Crear(Pedido(_ibuprofeno.Id), IdUsuarioDoctora, callerEsDoctor: true);

        // Como lo trae EF con el Include, el renglón apunta al medicamento.
        _recetas.Recetas.Single(r => r.Id == id).RecetaMedicamentos.Single().Medicamento = _ibuprofeno;

        // Alguien edita el medicamento después de emitida la receta.
        _ibuprofeno.Nombre = "IBUPIRAC FORTE";
        _ibuprofeno.Concentracion = "600 mg";

        var (receta, _, _) = await _logica.ObtenerPorId(id!.Value, IdUsuarioDoctora, callerEsStaff: true);

        var medicamento = receta!.Medicamentos.Single();
        Assert.Equal("IBUPIRAC", medicamento.Nombre);
        Assert.Equal("400 mg", medicamento.Concentracion);
    }

    [Fact]
    public async Task Una_receta_vieja_sin_copia_muestra_el_medicamento_como_esta_hoy()
    {
        // Emitida antes de este cambio: el renglón no tiene la copia.
        _recetas.Recetas.Add(new Receta
        {
            Id = 7,
            IdPaciente = IdPaciente,
            IdDoctor = 20,
            RecetaMedicamentos = new List<RecetaMedicamento>
            {
                new RecetaMedicamento
                {
                    IdMedicamento = _ibuprofeno.Id,
                    Medicamento = _ibuprofeno,
                    Dosis = "1 comprimido",
                    Frecuencia = "cada 8 horas"
                }
            }
        });

        var (receta, _, _) = await _logica.ObtenerPorId(7, IdUsuarioDoctora, callerEsStaff: true);

        var medicamento = receta!.Medicamentos.Single();
        Assert.Equal("IBUPIRAC", medicamento.Nombre);
        Assert.Equal("IBUPROFENO", medicamento.NombreGenerico);
        Assert.Equal("1 comprimido", medicamento.Dosis);
    }

    [Fact]
    public async Task La_receta_dice_quien_la_firmo()
    {
        var (id, _, _) = await _logica.Crear(Pedido(_ibuprofeno.Id), IdUsuarioDoctora, callerEsDoctor: true);

        // Como lo trae EF con el Include, la receta apunta a su doctor.
        var guardada = _recetas.Recetas.Single(r => r.Id == id);
        guardada.Doctor = _doctores.Doctores.Single(d => d.Id == guardada.IdDoctor);

        var (receta, _, _) = await _logica.ObtenerPorId(id!.Value, IdUsuarioDoctora, callerEsStaff: true);

        Assert.NotNull(receta!.Doctor);
        Assert.Equal(20, receta.Doctor.IdDoctor);
        Assert.Equal("Laura", receta.Doctor.Nombre);
        Assert.Equal("Méndez", receta.Doctor.Apellido);
        Assert.Equal("Clínica médica", receta.Doctor.Especialidad);
        Assert.Equal("MN 1234", receta.Doctor.Matricula);
    }

    [Fact]
    public async Task Al_modificar_la_receta_toma_los_datos_del_momento()
    {
        var (id, _, _) = await _logica.Crear(Pedido(_ibuprofeno.Id), IdUsuarioDoctora, callerEsDoctor: true);
        _ibuprofeno.Concentracion = "600 mg";

        // El doctor vuelve a firmar la receta con el mismo medicamento.
        var (ok, error, _) = await _logica.Actualizar(id!.Value, Pedido(_ibuprofeno.Id), IdUsuarioDoctora, callerEsDoctor: true);

        Assert.True(ok);
        Assert.Null(error);
        var renglon = _recetas.Recetas.Single(r => r.Id == id).RecetaMedicamentos.Single();
        Assert.Equal("600 mg", renglon.Concentracion);
    }
}
