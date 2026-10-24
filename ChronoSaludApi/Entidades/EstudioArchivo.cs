namespace ChronoSaludApi.Entidades;

// El PDF del resultado de un estudio (paso 23). Va en una tabla aparte, como
// la foto de perfil, para que el contenido no viaje cada vez que se lista un
// estudio. Siempre es un PDF: se revisa la firma del archivo al subirlo.
public class EstudioArchivo
{
    // Clave primaria y, a la vez, clave foránea a Estudio (un PDF por estudio)
    public int IdEstudio { get; set; }

    public byte[] Contenido { get; set; } = Array.Empty<byte>();

    // El nombre con el que se subió, para descargarlo con el mismo
    public string Nombre { get; set; } = string.Empty;

    public int Tamano { get; set; }

    public DateTime SubidoEn { get; set; }
}
