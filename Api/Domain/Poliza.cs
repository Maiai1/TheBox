namespace TheBox.Api.Domain;

public enum EstadoPoliza
{
    Vigente = 0,
    Suspendida = 1,
    Cancelada = 2
}

public class Poliza
{
    public int Id { get; set; }
    public int AseguradoId { get; set; }

    public string Numero { get; set; } = "";

    // Vehículo asegurado
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public string Patente { get; set; } = "";

    public string TipoSeguro { get; set; } = "";
    public EstadoPoliza Estado { get; set; } = EstadoPoliza.Vigente;

    public DateOnly VigenciaDesde { get; set; }
    public DateOnly VigenciaHasta { get; set; }

    public decimal PrimaMensual { get; set; }

    // true mientras tenga al menos una cuota vencida sin pagar.
    public bool EnMora { get; set; }

    // true si la póliza estuvo suspendida alguna vez por impagos (aunque ya esté reactivada).
    public bool TuvoSuspension { get; set; }

    public List<Cuota> Cuotas { get; set; } = [];
}