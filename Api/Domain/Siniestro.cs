namespace TheBox.Api.Domain;

public enum EstadoSiniestro
{
    Recibido = 0,
    EnAnalisis = 1,
    Cerrado = 2
}

public class Siniestro
{
    public int Id { get; set; }
    public int PolizaId { get; set; }
    public Poliza Poliza { get; set; } = null!;

    // Fecha en que ocurrió el incidente (no la de la denuncia).
    public DateOnly Fecha { get; set; }
    public string Tipo { get; set; } = "";
    public string Lugar { get; set; } = "";
    public string Descripcion { get; set; } = "";

    public EstadoSiniestro Estado { get; set; } = EstadoSiniestro.Recibido;
    public DateTime FechaDenuncia { get; set; } = DateTime.UtcNow;

    public List<DocumentoSiniestro> Documentos { get; set; } = [];
    public List<SeguimientoSiniestro> Seguimiento { get; set; } = [];
}

public class DocumentoSiniestro
{
    public int Id { get; set; }
    public int SiniestroId { get; set; }
    public string NombreOriginal { get; set; } = "";

    // Ruta relativa a la carpeta de almacenamiento (ej: "3f2a.../9c1b....pdf").
    public string RutaRelativa { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Bytes { get; set; }
}

// Historial de estados: cada cambio de estado agrega una fila.
public class SeguimientoSiniestro
{
    public int Id { get; set; }
    public int SiniestroId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoSiniestro Estado { get; set; }
    public string Comentario { get; set; } = "";
}