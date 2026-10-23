using System.Globalization;
using ChronoSaludApi.Entidades;
using ChronoSaludApi.Logica.DTOs;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Logica;

public class ReporteLogica : IReporteLogica
{
    /// <summary>Un año, bisiesto incluido: más allá el reporte deja de servir para comparar.</summary>
    public const int DiasMaximos = 366;

    public const string SinEspecialidad = "Sin especialidad";

    private static readonly StringComparer OrdenDeNombres =
        StringComparer.Create(CultureInfo.GetCultureInfo("es-AR"), ignoreCase: true);

    private readonly ITurnoRepository _turnoRepo;
    private readonly IDoctorRepository _doctorRepo;

    public ReporteLogica(ITurnoRepository turnoRepo, IDoctorRepository doctorRepo)
    {
        _turnoRepo = turnoRepo;
        _doctorRepo = doctorRepo;
    }

    public async Task<(ReporteResumenDto? reporte, string? error, bool noEncontrado, bool prohibido)> Resumen(
        DateTime? desde, DateTime? hasta, int? idDoctor, string? especialidad, Solicitante solicitante)
    {
        if (!desde.HasValue || !hasta.HasValue)
            return (null, "fecha_desde y fecha_hasta son requeridos.", false, false);

        var dia = desde.Value.Date;
        var hastaDia = hasta.Value.Date;

        if (hastaDia < dia)
            return (null, "fecha_hasta no puede ser anterior a fecha_desde.", false, false);
        if ((hastaDia - dia).TotalDays >= DiasMaximos)
            return (null, $"El período puede ser de hasta {DiasMaximos} días.", false, false);

        // El doctor ve solo lo suyo: su IdDoctor sale del token y pisa los
        // filtros que haya mandado, como en el historial de movimientos.
        Doctor? elegido = null;
        if (solicitante.EsDoctor)
        {
            elegido = await _doctorRepo.ObtenerPorIdUsuario(solicitante.IdUsuario);
            if (elegido == null)
                return (null, "Tu usuario no tiene un perfil de doctor asociado.", true, false);

            especialidad = null;
        }
        else if (!solicitante.EsAdministrador)
        {
            return (null, null, false, true);
        }
        else if (idDoctor.HasValue)
        {
            elegido = await _doctorRepo.ObtenerPorId(idDoctor.Value);
            if (elegido == null)
                return (null, "Doctor no encontrado.", true, false);
        }

        especialidad = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim();

        var filtro = new FiltroTurnos
        {
            DoctorId = elegido?.Id,
            Desde = dia,
            Hasta = hastaDia,
            Especialidad = especialidad
        };

        var conteos = await _turnoRepo.ContarPorDoctorYEstado(filtro);
        var atendidos = await _turnoRepo.ContarPacientesAtendidos(filtro);

        var doctores = await DoctoresDelReporte(elegido, especialidad, conteos);

        var porDoctor = doctores
            .Select(d => new ReporteDoctorDto(
                d.Id,
                NombreDe(d),
                EspecialidadDe(d),
                d.Activo,
                Contar(conteos.Where(c => c.IdDoctor == d.Id))))
            .OrderByDescending(d => d.Turnos.Total)
            .ThenBy(d => d.Doctor, OrdenDeNombres)
            .ToList();

        // Las especialidades se juntan sin mirar mayúsculas, como el filtro.
        var porEspecialidad = porDoctor
            .GroupBy(d => d.Especialidad, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => new ReporteEspecialidadDto(ComoSeEscribe(grupo.Select(d => d.Especialidad)), Sumar(grupo.Select(d => d.Turnos))))
            .OrderByDescending(e => e.Turnos.Total)
            .ThenBy(e => e.Especialidad, OrdenDeNombres)
            .ToList();

        return (new ReporteResumenDto(
            dia,
            hastaDia,
            elegido?.Id,
            especialidad,
            Contar(conteos),
            atendidos,
            porEspecialidad,
            porDoctor), null, false, false);
    }

    /// <summary>
    /// Los doctores que van en el desglose: el elegido o, si no hay, todos
    /// los activos (de la especialidad, si se filtró), también los que no
    /// tuvieron turnos. Se suman los dados de baja que tuvieron turnos en el
    /// período, para que el desglose sume lo mismo que el total.
    /// </summary>
    private async Task<List<Doctor>> DoctoresDelReporte(Doctor? elegido, string? especialidad, List<ConteoDeTurnos> conteos)
    {
        if (elegido != null)
            return new List<Doctor> { elegido };

        var doctores = (await _doctorRepo.ObtenerTodos(null, null))
            .Where(d => especialidad == null || string.Equals(d.Especialidad?.Trim(), especialidad, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var faltan = conteos
            .Select(c => c.IdDoctor)
            .Distinct()
            .Where(id => doctores.All(d => d.Id != id))
            .ToList();

        foreach (var id in faltan)
        {
            var doctor = await _doctorRepo.ObtenerPorId(id);
            doctores.Add(doctor ?? new Doctor { Id = id, Activo = false });
        }

        return doctores;
    }

    public static ConteoTurnosDto Contar(IEnumerable<ConteoDeTurnos> conteos)
    {
        var lista = conteos.ToList();
        int De(string estado) => lista.Where(c => c.Estado == estado).Sum(c => c.Cantidad);

        return Armar(
            lista.Sum(c => c.Cantidad),
            De(EstadosTurno.Pendiente),
            De(EstadosTurno.Confirmado),
            De(EstadosTurno.Completado),
            De(EstadosTurno.Ausente),
            De(EstadosTurno.Cancelado));
    }

    private static ConteoTurnosDto Sumar(IEnumerable<ConteoTurnosDto> conteos)
    {
        var lista = conteos.ToList();
        return Armar(
            lista.Sum(c => c.Total),
            lista.Sum(c => c.Pendientes),
            lista.Sum(c => c.Confirmados),
            lista.Sum(c => c.Completados),
            lista.Sum(c => c.Ausentes),
            lista.Sum(c => c.Cancelados));
    }

    private static ConteoTurnosDto Armar(int total, int pendientes, int confirmados, int completados, int ausentes, int cancelados) =>
        new(total, pendientes, confirmados, completados, ausentes, cancelados, Ausentismo(completados, ausentes));

    /// <summary>El porcentaje de ausentes entre completados y ausentes, con un decimal; null si no hay ninguno.</summary>
    public static double? Ausentismo(int completados, int ausentes)
    {
        var cerrados = completados + ausentes;
        return cerrados == 0 ? null : Math.Round(100.0 * ausentes / cerrados, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Si la misma especialidad está escrita de dos formas ("Clínica" y
    /// "clínica"), va la más repetida; si empatan, la que lleva mayúscula.
    /// </summary>
    private static string ComoSeEscribe(IEnumerable<string> formas) =>
        formas.GroupBy(f => f, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .First().Key;

    private static string NombreDe(Doctor doctor)
    {
        var nombre = $"{doctor.Usuario?.Nombre} {doctor.Usuario?.Apellido}".Trim();
        return string.IsNullOrEmpty(nombre) ? $"Doctor #{doctor.Id}" : nombre;
    }

    private static string EspecialidadDe(Doctor doctor) =>
        string.IsNullOrWhiteSpace(doctor.Especialidad) ? SinEspecialidad : doctor.Especialidad.Trim();
}
