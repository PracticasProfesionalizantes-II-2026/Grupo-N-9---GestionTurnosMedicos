using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChronoSaludWeb.Models;

public class PacienteFilaViewModel
{
    public int IdPaciente { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;

    public string NombreCompleto =>
        string.IsNullOrWhiteSpace($"{Nombre}{Apellido}") ? "Sin datos" : $"{Nombre} {Apellido}".Trim();

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(NombreCompleto);
}

public class PacientesIndexViewModel
{
    public IReadOnlyList<PacienteFilaViewModel> Pacientes { get; init; } = Array.Empty<PacienteFilaViewModel>();
    public int Total { get; init; }

    /// <summary>Texto del buscador. La API lo resuelve como "contiene".</summary>
    public string? Nombre { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public bool HayFiltro => !string.IsNullOrWhiteSpace(Nombre);
    public bool SinResultados => Pacientes.Count == 0 && HayFiltro;
    public bool HayMas => Total > Pacientes.Count;
}

public class CoberturaFilaViewModel
{
    public string NombreCobertura { get; init; } = string.Empty;
    public string? Plan { get; init; }
    public string IdAfiliado { get; init; } = string.Empty;
}

public class PacienteDetalleViewModel
{
    public int IdPaciente { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public DateTime? FechaNacimiento { get; init; }
    public string? Sexo { get; init; }
    public string? GrupoSanguineo { get; init; }
    public string? Alergias { get; init; }
    public string? Condiciones { get; init; }
    public IReadOnlyList<CoberturaFilaViewModel> Coberturas { get; init; } = Array.Empty<CoberturaFilaViewModel>();

    /// <summary>Fecha del turno más reciente del paciente, si tiene alguno.</summary>
    public DateTime? UltimoTurno { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public string? UltimoTurnoLargo =>
        UltimoTurno?.ToString("d 'de' MMMM 'de' yyyy", TurnosIndexViewModel.Cultura);

    public string NombreCompleto =>
        string.IsNullOrWhiteSpace($"{Nombre}{Apellido}") ? "Sin datos" : $"{Nombre} {Apellido}".Trim();

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(NombreCompleto);

    public string? FechaNacimientoLarga =>
        FechaNacimiento?.ToString("d 'de' MMMM 'de' yyyy", TurnosIndexViewModel.Cultura);

    /// <summary>Edad en años cumplidos, si la API mandó la fecha de nacimiento.</summary>
    public int? Edad
    {
        get
        {
            if (FechaNacimiento is not { } nacimiento) return null;

            var edad = DateTime.Today.Year - nacimiento.Year;
            if (nacimiento.Date > DateTime.Today.AddYears(-edad)) edad--;
            return edad < 0 ? null : edad;
        }
    }
}

public class DoctorFilaViewModel
{
    public int IdDoctor { get; init; }

    /// <summary>La API ya lo manda como "Nombre Apellido".</summary>
    public string Nombre { get; init; } = string.Empty;
    public string Especialidad { get; init; } = string.Empty;
    public string Matricula { get; init; } = string.Empty;

    public string NombreMostrado =>
        string.IsNullOrWhiteSpace(Nombre) ? "Sin datos" : Nombre.Trim();

    public string? EspecialidadMostrada =>
        string.IsNullOrWhiteSpace(Especialidad) ? null : Especialidad.Trim();

    public string? MatriculaMostrada =>
        string.IsNullOrWhiteSpace(Matricula) ? null : Matricula.Trim();

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(NombreMostrado);
}

public class DoctoresIndexViewModel
{
    public IReadOnlyList<DoctorFilaViewModel> Doctores { get; init; } = Array.Empty<DoctorFilaViewModel>();
    public int Total { get; init; }

    public string? Especialidad { get; init; }

    /// <summary>
    /// Opciones del select. La API no expone un endpoint de especialidades, así
    /// que salen de la propia lista de doctores.
    /// </summary>
    public IReadOnlyList<SelectListItem> Especialidades { get; init; } = Array.Empty<SelectListItem>();

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public bool HayFiltro => !string.IsNullOrWhiteSpace(Especialidad);
    public bool SinResultados => Doctores.Count == 0 && HayFiltro;
    public bool HayMas => Total > Doctores.Count;
}

public class DoctorDetalleViewModel
{
    public int IdDoctor { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Especialidad { get; init; } = string.Empty;
    public string Matricula { get; init; } = string.Empty;
    public string? Consultorio { get; init; }

    public string? Error { get; init; }
    public bool HuboError => Error is not null;

    public string NombreCompleto =>
        string.IsNullOrWhiteSpace($"{Nombre}{Apellido}") ? "Sin datos" : $"{Nombre} {Apellido}".Trim();

    public string Iniciales => TurnosIndexViewModel.CalcularIniciales(NombreCompleto);
}
