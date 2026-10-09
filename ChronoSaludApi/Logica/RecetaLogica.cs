using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class RecetaLogica : IRecetaLogica
{
    private readonly IRecetaRepository    _repo;
    private readonly IMedicamentoRepository _medRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IDoctorRepository _doctorRepo;

    public RecetaLogica(
        IRecetaRepository repo,
        IMedicamentoRepository medRepo,
        IPacienteRepository pacienteRepo,
        IDoctorRepository doctorRepo)
    {
        _repo    = repo;
        _medRepo = medRepo;
        _pacienteRepo = pacienteRepo;
        _doctorRepo = doctorRepo;
    }

    public async Task<(IEnumerable<RecetaDto> recetas, string? error, bool prohibido)> ObtenerDePaciente(
        int pacienteId, int idUsuarioCaller, bool callerEsStaff)
    {
        if (!callerEsStaff)
        {
            var propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (Enumerable.Empty<RecetaDto>(), "Tu usuario no tiene un perfil de paciente asociado.", false);
            if (propio.Id != pacienteId)
                return (Enumerable.Empty<RecetaDto>(), null, true);
        }

        var recetas = await _repo.ObtenerDePaciente(pacienteId);
        return (recetas.Select(MapearDto), null, false);
    }

    public async Task<(RecetaDto? receta, string? error, bool prohibido)> ObtenerPorId(
        int id, int idUsuarioCaller, bool callerEsStaff)
    {
        // Mismo criterio que ObtenerDePaciente: el staff ve cualquier receta y
        // el paciente solo las propias. El perfil se resuelve antes de buscar la
        // receta para que "no tenés perfil" no dependa de que el id exista.
        Paciente? propio = null;
        if (!callerEsStaff)
        {
            propio = await _pacienteRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (propio == null)
                return (null, "Tu usuario no tiene un perfil de paciente asociado.", false);
        }

        var r = await _repo.ObtenerPorId(id);
        if (r == null) return (null, "Receta no encontrada.", false);

        if (propio != null && propio.Id != r.IdPaciente)
            return (null, null, true);

        return (MapearDto(r), null, false);
    }

    public async Task<(int? id, string? error, bool sinPerfilDoctor)> Crear(
        RecetaCreateDto dto, int idUsuarioCaller, bool callerEsDoctor)
    {
        if (dto.Medicamentos == null || !dto.Medicamentos.Any())
            return (null, "Debe especificar al menos un medicamento.", false);

        // Quién firma la receta: el doctor la emite siempre a su propio nombre
        // (lo que venga en el cuerpo se ignora) y el staff la emite a nombre del
        // doctor que indique explícitamente.
        var idDoctor = dto.IdDoctor;
        if (callerEsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (doctor == null)
                return (null, "Tu usuario no tiene un perfil de doctor asociado.", true);
            idDoctor = doctor.Id;
        }
        else
        {
            // Sin esto, un id_doctor inventado se iba contra la foreign key y
            // salía como 500. El del doctor no hace falta chequearlo: viene de
            // su propio perfil, que ya se resolvió recién.
            var doctor = await _doctorRepo.ObtenerPorId(idDoctor);
            if (doctor == null)
                return (null, $"Doctor con id {idDoctor} no encontrado.", false);
        }

        // Cada medicamento tiene que existir. Cada renglón guarda una copia de
        // sus datos, así la receta no cambia si después se edita el medicamento.
        var renglones = new List<RecetaMedicamento>();
        foreach (var m in dto.Medicamentos)
        {
            var med = await _medRepo.ObtenerPorId(m.IdMedicamento);
            if (med == null)
                return (null, $"Medicamento con id {m.IdMedicamento} no encontrado.", false);

            renglones.Add(ArmarRenglon(m, med));
        }

        var receta = new Receta
        {
            IdPaciente = dto.IdPaciente,
            IdDoctor   = idDoctor,
            IdTurno    = dto.IdTurno,
            Fecha      = dto.Fecha,
            Vigencia   = dto.Vigencia,
            Detalles   = dto.Detalles,
            RecetaMedicamentos = renglones
        };

        await _repo.Agregar(receta);
        return (receta.Id, null, false);
    }

    public async Task<(bool ok, string? error, bool sinPerfilDoctor)> Actualizar(
        int id, RecetaCreateDto dto, int idUsuarioCaller, bool callerEsDoctor)
    {
        var receta = await _repo.ObtenerPorId(id);
        if (receta == null) return (false, "Receta no encontrada.", false);

        // El doctor solo edita las que firmó él. A una receta de otro doctor se
        // le responde lo mismo que a una inexistente: no confirmamos que exista.
        if (callerEsDoctor)
        {
            var doctor = await _doctorRepo.ObtenerPorIdUsuario(idUsuarioCaller);
            if (doctor == null)
                return (false, "Tu usuario no tiene un perfil de doctor asociado.", true);
            if (doctor.Id != receta.IdDoctor)
                return (false, "Receta no encontrada.", false);
        }

        if (dto.Medicamentos == null || !dto.Medicamentos.Any())
            return (false, "Debe especificar al menos un medicamento.", false);

        // Los renglones se reemplazan y toman los datos que tiene cada
        // medicamento ahora: el doctor vuelve a firmar la receta.
        var renglones = new List<RecetaMedicamento>();
        foreach (var m in dto.Medicamentos)
        {
            var med = await _medRepo.ObtenerPorId(m.IdMedicamento);
            if (med == null)
                return (false, $"Medicamento con id {m.IdMedicamento} no encontrado.", false);

            var renglon = ArmarRenglon(m, med);
            renglon.IdReceta = id;
            renglones.Add(renglon);
        }

        receta.IdTurno   = dto.IdTurno;
        receta.Fecha     = dto.Fecha;
        receta.Vigencia  = dto.Vigencia;
        receta.Detalles  = dto.Detalles;
        receta.RecetaMedicamentos = renglones;

        await _repo.Actualizar(receta);
        return (true, null, false);
    }

    /// <summary>
    /// Un renglón de la receta: lo que indicó el doctor más una copia de los
    /// datos del medicamento tal como están hoy.
    /// </summary>
    private static RecetaMedicamento ArmarRenglon(RecetaMedicamentoDto m, Medicamento med) => new RecetaMedicamento
    {
        IdMedicamento     = m.IdMedicamento,
        Dosis             = m.Dosis,
        Frecuencia        = m.Frecuencia,
        Duracion          = m.Duracion,
        Indicaciones      = m.Indicaciones,
        NombreMedicamento = med.Nombre,
        NombreGenerico    = med.NombreGenerico,
        Concentracion     = med.Concentracion,
        FormaFarmaceutica = med.FormaFarmaceutica
    };

    private static RecetaDto MapearDto(Receta r) => new RecetaDto(
        r.Id,
        r.Fecha,
        r.Vigencia,
        r.Detalles,
        r.RecetaMedicamentos.Select(MapearRenglon).ToList(),
        Profesional.Armar(r.Doctor)
    );

    private static MedicamentoRecetadoDto MapearRenglon(RecetaMedicamento rm)
    {
        // Un renglón sin copia es de una receta emitida antes de este cambio:
        // se muestra el medicamento como está hoy.
        if (rm.NombreMedicamento == null && rm.Medicamento != null)
        {
            return new MedicamentoRecetadoDto(
                rm.IdMedicamento,
                rm.Medicamento.Nombre,
                rm.Medicamento.NombreGenerico,
                rm.Medicamento.Concentracion,
                rm.Medicamento.FormaFarmaceutica,
                rm.Dosis,
                rm.Frecuencia,
                rm.Duracion,
                rm.Indicaciones);
        }

        return new MedicamentoRecetadoDto(
            rm.IdMedicamento,
            rm.NombreMedicamento ?? "",
            rm.NombreGenerico,
            rm.Concentracion,
            rm.FormaFarmaceutica,
            rm.Dosis,
            rm.Frecuencia,
            rm.Duracion,
            rm.Indicaciones);
    }
}
