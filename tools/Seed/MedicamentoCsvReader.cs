namespace Seed;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

public record MedicamentoCsv(
    string NombreComercial,
    string NombreGenerico,
    string Concentracion,
    string FormaFarmaceutica,
    string Laboratorio);

public static class MedicamentoCsvReader
{
    // Lee todos los .csv de la carpeta y devuelve la lista sin repetidos.
    public static List<MedicamentoCsv> LeerCarpeta(string carpeta)
    {
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultado = new List<MedicamentoCsv>();

        foreach (var archivo in Directory.GetFiles(carpeta, "*.csv").OrderBy(f => f))
        {
            foreach (var med in LeerArchivo(archivo))
            {
                var clave = $"{med.NombreComercial}|{med.NombreGenerico}|{med.Concentracion}|{med.FormaFarmaceutica}";
                if (vistos.Add(clave))
                    resultado.Add(med);
            }
        }

        return resultado;
    }

    private static IEnumerable<MedicamentoCsv> LeerArchivo(string ruta)
    {
        using var parser = new TextFieldParser(ruta, new UTF8Encoding(true));
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        if (parser.EndOfData) yield break;

        var encabezados = parser.ReadFields() ?? Array.Empty<string>();
        int iComercial = BuscarColumna(encabezados, "COMERCIAL");
        int iGenerico = BuscarColumna(encabezados, "GENERICO");
        int iConcentracion = BuscarColumna(encabezados, "CONCENTRACION");
        int iForma = BuscarColumna(encabezados, "FORMA");
        int iLaboratorio = BuscarColumna(encabezados, "LABORATORIO");

        if (iComercial < 0 || iGenerico < 0)
            throw new InvalidDataException(
                $"{Path.GetFileName(ruta)}: no encontré las columnas de nombre comercial y genérico.");

        while (!parser.EndOfData)
        {
            var campos = parser.ReadFields();
            if (campos is null) continue;

            var comercial = Campo(campos, iComercial);
            var generico = Campo(campos, iGenerico);
            if (comercial.Length == 0 || generico.Length == 0) continue;

            yield return new MedicamentoCsv(
                comercial,
                generico,
                Campo(campos, iConcentracion),
                Campo(campos, iForma),
                Campo(campos, iLaboratorio));
        }
    }

    // Busca la columna cuyo encabezado contiene la clave, ignorando tildes y mayúsculas.
    // Sirve tanto para "NOMBRE COMERCIAL" como para "nombre_comercial".
    private static int BuscarColumna(string[] encabezados, string clave) =>
        Array.FindIndex(encabezados, h => QuitarTildes(h).ToUpperInvariant().Contains(clave));

    private static string Campo(string[] campos, int indice) =>
        indice >= 0 && indice < campos.Length ? Limpiar(campos[indice]) : "";

    // El CSV trae espacios dobles ("IBUPROFENO  -  CAFEINA"): se juntan en uno.
    private static string Limpiar(string texto) =>
        Regex.Replace(texto, @"\s+", " ").Trim();

    private static string QuitarTildes(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in descompuesto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}