using ChronoSaludApi.Logica;
using ChronoSaludApi.Repositorios;

namespace ChronoSaludApi.Endpoints;

public static class ReporteEndpoints
{
    public static void MapReporteEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/reportes")
            .WithTags("Reportes")
            .RequireAuthorization(p => p.RequireRole("administrador", "doctor"));

        // GET /reportes/turnos
        grupo.MapGet("/turnos", async (
            ITurnoLogica logica,
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            int? doctor_id,
            string? especialidad,
            string? formato) =>
        {
            if (!fecha_desde.HasValue || !fecha_hasta.HasValue)
                return Results.BadRequest(new { error = "fecha_desde y fecha_hasta son requeridos." });

            var (total, turnos, _) = await logica.ObtenerTodos(
                null, doctor_id, null, fecha_desde, fecha_hasta, 1, int.MaxValue,
                idUsuarioCaller: 0, callerEsPaciente: false, callerEsDoctor: false);

            var completados = turnos.Count(t => t.Estado == "completado");
            var cancelados  = turnos.Count(t => t.Estado == "cancelado");
            var pendientes  = turnos.Count(t => t.Estado == "pendiente");

            return Results.Ok(new
            {
                total_turnos = total,
                completados,
                cancelados,
                pendientes,
                periodo = new { desde = fecha_desde, hasta = fecha_hasta }
            });
        })
        .WithSummary("Reporte estadístico de turnos");

        // GET /reportes/pacientes
        grupo.MapGet("/pacientes", async (
            IPacienteRepository pacienteRepo,
            ITurnoRepository turnoRepo,
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            string? formato) =>
        {
            if (!fecha_desde.HasValue || !fecha_hasta.HasValue)
                return Results.BadRequest(new { error = "fecha_desde y fecha_hasta son requeridos." });

            var totalPacientes = (await pacienteRepo.ObtenerTodos(null, null)).Count();

            var turnosPeriodo = (await turnoRepo.ObtenerTodos(null, null, null, fecha_desde, fecha_hasta)).ToList();
            var pacientesAtendidos = turnosPeriodo
                .Where(t => t.Estado == "completado")
                .Select(t => t.IdPaciente)
                .Distinct()
                .Count();

            return Results.Ok(new
            {
                mensaje = "Reporte de pacientes generado.",
                periodo = new { desde = fecha_desde, hasta = fecha_hasta },
                total_pacientes = totalPacientes,
                turnos_en_periodo = turnosPeriodo.Count,
                pacientes_atendidos_en_periodo = pacientesAtendidos
            });
        })
        .WithSummary("Reporte de actividad de pacientes")
        .RequireAuthorization(p => p.RequireRole("administrador"));

        // GET /reportes/disponibilidad
        grupo.MapGet("/disponibilidad", async (
            IDoctorRepository doctorRepo,
            ITurnoRepository turnoRepo,
            int? doctor_id,
            DateTime? fecha_desde,
            DateTime? fecha_hasta) =>
        {
            if (!fecha_desde.HasValue || !fecha_hasta.HasValue)
                return Results.BadRequest(new { error = "fecha_desde y fecha_hasta son requeridos." });

            // Nivel 2 acotado: Doctor no tiene ningún campo de horario laboral,
            // así que se reporta ocupación real (turnos no cancelados), no huecos libres.
            var turnosPeriodo = await turnoRepo.ObtenerTodos(null, doctor_id, null, fecha_desde, fecha_hasta);
            var ocupadosPorDoctor = turnosPeriodo
                .Where(t => t.Estado != "cancelado")
                .GroupBy(t => t.IdDoctor)
                .ToDictionary(g => g.Key, g => g.Count());

            if (doctor_id.HasValue)
            {
                return Results.Ok(new
                {
                    mensaje    = "Reporte de disponibilidad generado.",
                    doctor_id,
                    periodo = new { desde = fecha_desde, hasta = fecha_hasta },
                    turnos_ocupados = ocupadosPorDoctor.GetValueOrDefault(doctor_id.Value, 0)
                });
            }

            var doctores = await doctorRepo.ObtenerTodos(null, null);
            var porDoctor = doctores.Select(d => new
            {
                doctor_id     = d.Id,
                doctor        = $"{d.Usuario?.Nombre} {d.Usuario?.Apellido}",
                especialidad  = d.Especialidad,
                turnos_ocupados = ocupadosPorDoctor.GetValueOrDefault(d.Id, 0)
            });

            return Results.Ok(new
            {
                mensaje    = "Reporte de disponibilidad generado.",
                doctor_id  = (int?)null,
                periodo = new { desde = fecha_desde, hasta = fecha_hasta },
                turnos_ocupados_por_doctor = porDoctor
            });
        })
        .WithSummary("Reporte de disponibilidad de profesionales");
    }
}
