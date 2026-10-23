namespace ChronoSaludWeb.Services;

/// <summary>
/// Cuántos turnos hay en cada estado. Espeja ConteoTurnosDto. Ausentismo es
/// el porcentaje de ausentes entre completados y ausentes, con un decimal;
/// null si no hubo ninguno de los dos.
/// </summary>
public record ConteoTurnos(
    int Total,
    int Pendientes,
    int Confirmados,
    int Completados,
    int Ausentes,
    int Cancelados,
    double? Ausentismo);

public record ReporteEspecialidad(string Especialidad, ConteoTurnos Turnos);

/// <summary>Activo es false para un doctor dado de baja que tuvo turnos en el período.</summary>
public record ReporteDoctor(int IdDoctor, string Doctor, string Especialidad, bool Activo, ConteoTurnos Turnos);

/// <summary>Respuesta de GET /reportes/resumen. Espeja ReporteResumenDto.</summary>
public record ReporteResumen(
    DateTime Desde,
    DateTime Hasta,
    int? IdDoctor,
    string? Especialidad,
    ConteoTurnos Turnos,
    int PacientesAtendidos,
    IReadOnlyList<ReporteEspecialidad> PorEspecialidad,
    IReadOnlyList<ReporteDoctor> PorDoctor);

/// <summary>Lectura de los reportes. Solo para administrador y doctor.</summary>
public class ReporteService
{
    private readonly ApiClient _api;

    public ReporteService(ApiClient api) => _api = api;

    /// <summary>
    /// GET /reportes/resumen. <paramref name="desde"/> y <paramref name="hasta"/>
    /// son días, los dos incluidos. Al doctor la API le ignora el doctor y la
    /// especialidad y le devuelve lo suyo. Deja pasar la ApiException: 400 por
    /// un período mal armado, 404 para un doctor sin perfil.
    /// </summary>
    public async Task<ReporteResumen?> ResumenAsync(DateTime desde, DateTime hasta, int? doctorId = null, string? especialidad = null)
    {
        var parametros = new Dictionary<string, object?>
        {
            ["fecha_desde"]  = desde.Date,
            ["fecha_hasta"]  = hasta.Date,
            ["doctor_id"]    = doctorId,
            ["especialidad"] = especialidad,
        };

        return await _api.GetAsync<ReporteResumen>("/reportes/resumen", parametros);
    }
}
