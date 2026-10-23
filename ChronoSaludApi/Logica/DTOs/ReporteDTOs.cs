namespace ChronoSaludApi.Logica.DTOs;

/// <summary>
/// Cuántos turnos hay en cada estado. Ausentismo es el porcentaje de ausentes
/// entre los turnos que ya se cerraron con el paciente (completados más
/// ausentes), con un decimal: los pendientes, confirmados y cancelados no
/// cuentan. Null si no hay ninguno cerrado.
/// </summary>
public record ConteoTurnosDto(
    int Total,
    int Pendientes,
    int Confirmados,
    int Completados,
    int Ausentes,
    int Cancelados,
    double? Ausentismo
);

public record ReporteEspecialidadDto(string Especialidad, ConteoTurnosDto Turnos);

/// <summary>Activo es false para un doctor dado de baja que tuvo turnos en el período.</summary>
public record ReporteDoctorDto(int IdDoctor, string Doctor, string Especialidad, bool Activo, ConteoTurnosDto Turnos);

/// <summary>
/// El reporte de un período: los totales, cuántos pacientes distintos se
/// atendieron y el desglose por especialidad y por doctor, de más turnos a
/// menos. IdDoctor y Especialidad son los filtros que se aplicaron; al doctor
/// le llega siempre su propio IdDoctor.
/// </summary>
public record ReporteResumenDto(
    DateTime Desde,
    DateTime Hasta,
    int? IdDoctor,
    string? Especialidad,
    ConteoTurnosDto Turnos,
    int PacientesAtendidos,
    List<ReporteEspecialidadDto> PorEspecialidad,
    List<ReporteDoctorDto> PorDoctor
);
