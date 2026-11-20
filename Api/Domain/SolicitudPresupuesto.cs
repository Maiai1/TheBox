namespace TheBox.Api.Domain;

public enum EstadoSolicitud
{
    Pendiente = 0,
    EnEvaluacion = 1,
    Aprobada = 2,
    Rechazada = 3
}

public class SolicitudPresupuesto
{
    public int Id { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    // Toda solicitud nueva queda pendiente de evaluación por el asesor.
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    // Datos de contacto
    public string Nombre { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";

    // Vehículo
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public string Patente { get; set; } = "";
    public string Cobertura { get; set; } = "";

    // Declaración Jurada
    public bool DdjjAceptada { get; set; }
    public DateTime DdjjFecha { get; set; }
    public bool TieneGnc { get; set; }
    public bool SiniestrosUltimos3Anios { get; set; }
    public bool UsoComercial { get; set; }

    public List<FotoSolicitud> Fotos { get; set; } = [];
}

public class FotoSolicitud
{
    public int Id { get; set; }
    public int SolicitudPresupuestoId { get; set; }
    public string NombreOriginal { get; set; } = "";

    // Ruta relativa a la carpeta de almacenamiento (ej: "3f2a.../9c1b....jpg").
    public string RutaRelativa { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Bytes { get; set; }
}

