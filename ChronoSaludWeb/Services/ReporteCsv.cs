using System.Globalization;
using System.Text;

namespace ChronoSaludWeb.Services;

/// <summary>
/// El reporte en CSV, para abrirlo en Excel: lo mismo que se ve en pantalla,
/// en tres bloques (totales, por especialidad y por doctor). Va separado por
/// punto y coma y con la coma decimal, que es lo que espera Excel en
/// castellano, y en UTF-8 con BOM para que respete los acentos.
/// </summary>
public static class ReporteCsv
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    private static readonly string[] Columnas =
        { "Turnos", "Pendientes", "Confirmados", "Completados", "Ausentes", "Cancelados", "Ausentismo (%)" };

    /// <summary>"reporte-turnos-2026-10-01-al-2026-10-31.csv".</summary>
    public static string NombreDeArchivo(DateTime desde, DateTime hasta) =>
        $"reporte-turnos-{desde:yyyy-MM-dd}-al-{hasta:yyyy-MM-dd}.csv";

    /// <summary>Los bytes del archivo, con el BOM adelante.</summary>
    public static byte[] Bytes(string contenido) =>
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(contenido)).ToArray();

    /// <summary>
    /// <paramref name="deQuien"/> es el doctor o la especialidad elegidos, si
    /// hay; <paramref name="deUnDoctor"/> deja afuera el bloque por especialidad,
    /// que con un solo doctor repite los totales.
    /// </summary>
    public static string Armar(ReporteResumen reporte, string periodo, string? deQuien, bool deUnDoctor)
    {
        var csv = new StringBuilder();
        void Fila(params string?[] valores) => csv.Append(string.Join(';', valores.Select(Celda))).Append("\r\n");

        Fila("Reporte de turnos", periodo);
        if (deQuien is not null) Fila("De", deQuien);
        Fila();

        var t = reporte.Turnos;
        Fila("Estado", "Turnos");
        Fila("Pendientes", Numero(t.Pendientes));
        Fila("Confirmados", Numero(t.Confirmados));
        Fila("Completados", Numero(t.Completados));
        Fila("Ausentes", Numero(t.Ausentes));
        Fila("Cancelados", Numero(t.Cancelados));
        Fila("Total", Numero(t.Total));
        Fila("Pacientes atendidos", Numero(reporte.PacientesAtendidos));
        Fila("Ausentismo (%)", Porcentaje(t.Ausentismo));

        if (!deUnDoctor && reporte.PorEspecialidad.Count > 0)
        {
            Fila();
            Fila(new[] { "Especialidad" }.Concat(Columnas).ToArray());
            foreach (var fila in reporte.PorEspecialidad)
                Fila(new[] { fila.Especialidad }.Concat(Conteo(fila.Turnos)).ToArray());
        }

        if (reporte.PorDoctor.Count > 0)
        {
            Fila();
            Fila(new[] { "Doctor", "Especialidad" }.Concat(Columnas).ToArray());
            foreach (var fila in reporte.PorDoctor)
            {
                var nombre = fila.Activo ? fila.Doctor : $"{fila.Doctor} (dado de baja)";
                Fila(new[] { nombre, fila.Especialidad }.Concat(Conteo(fila.Turnos)).ToArray());
            }
        }

        return csv.ToString();
    }

    private static IEnumerable<string> Conteo(ConteoTurnos t) => new[]
    {
        Numero(t.Total), Numero(t.Pendientes), Numero(t.Confirmados), Numero(t.Completados),
        Numero(t.Ausentes), Numero(t.Cancelados), Porcentaje(t.Ausentismo)
    };

    private static string Numero(int valor) => valor.ToString(CultureInfo.InvariantCulture);

    /// <summary>"33,3"; vacío si no hubo turnos cerrados, para que Excel no lo tome como cero.</summary>
    private static string Porcentaje(double? valor) => valor?.ToString("0.0", Cultura) ?? string.Empty;

    /// <summary>
    /// Entre comillas si trae punto y coma, comillas o saltos de línea. Un
    /// texto que empieza con = + - @ lleva un apóstrofo adelante: Excel lo
    /// tomaría como una fórmula, y los nombres los escribe cualquiera.
    /// </summary>
    public static string Celda(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;

        if ("=+-@\t\r".Contains(valor[0]) && !double.TryParse(valor, NumberStyles.Float, Cultura, out _))
            valor = "'" + valor;

        return valor.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;
    }
}
