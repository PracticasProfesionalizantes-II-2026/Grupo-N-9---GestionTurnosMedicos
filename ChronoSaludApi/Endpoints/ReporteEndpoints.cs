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
            ITurnoRepository turnoRepo,
            DateTime? fecha_desde,
            DateTime? fecha_hasta,
            int? doctor_id,
            string? especialidad,
            string? formato) =>
        {
            if (!fecha_desde.HasValue || !fecha_hasta.HasValue)
                return Results.BadRequest(new { error = "fecha_desde y fecha_hasta son requeridos." });

            // Cuenta la base: no se trae ningún turno.
            var filtro = new FiltroTurnos { DoctorId = doctor_id, Desde = fecha_desde, Hasta = fecha_hasta };
            var conteos = await turnoRepo.ContarPorEstado(filtro);

            return Results.Ok(new
            {
                total_turnos = conteos.Values.Sum(),
                completados  = conteos["completado"],
                cancelados   = conteos["cancelado"],
                pendientes   = conteos["pendiente"],
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

            // Todo se cuenta en la base. Del listado de pacientes alcanza con
            // el total: se pide una página de uno.
            var (totalPacientes, _) = await pacienteRepo.Buscar(null, null, null, 1, 1);

            var filtro = new FiltroTurnos { Desde = fecha_desde, Hasta = fecha_hasta };
            var turnosEnPeriodo = (await turnoRepo.ContarPorEstado(filtro)).Values.Sum();
            var pacientesAtendidos = await turnoRepo.ContarPacientesAtendidos(fecha_desde.Value, fecha_hasta.Value);

            return Results.Ok(new
            {
                mensaje = "Reporte de pacientes generado.",
                periodo = new { desde = fecha_desde, hasta = fecha_hasta },
                total_pacientes = totalPacientes,
                turnos_en_periodo = turnosEnPeriodo,
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
            var ocupadosPorDoctor = await turnoRepo.ContarOcupadosPorDoctor(doctor_id, fecha_desde.Value, fecha_hasta.Value);

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
