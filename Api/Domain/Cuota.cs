namespace TheBox.Api.Domain;

public enum EstadoCuota
{
    Pendiente = 0,
    Pagada = 1
}

public enum EstadoPago
{
    Aprobado = 0,
    Rechazado = 1,
    Pendiente = 2
}

public class Cuota
{
    public int Id { get; set; }
    public int PolizaId { get; set; }
    public Poliza Poliza { get; set; } = null!;

    public int Numero { get; set; }
    public DateOnly Vencimiento { get; set; }

    // Fecha que tenía la cuota al generarse el cronograma. El margen de postergación se mide contra esta
    // fecha, así que los cambios sucesivos nunca pueden superar los 15 días desde el original.
    public DateOnly VencimientoOriginal { get; set; }
    public decimal Monto { get; set; }

    // Solo se guarda Pendiente o Pagada. "Vencida" se calcula al consultar (Pendiente con fecha pasada).
    public EstadoCuota Estado { get; set; } = EstadoCuota.Pendiente;
    public DateTime? FechaPago { get; set; }
}

// Un intento de cobro. De la tarjeta se guarda solo una descripción ("Visa terminada en 4242"),
// nunca el número completo ni el código de seguridad.
public class Pago
{
    public int Id { get; set; }
    public int CuotaId { get; set; }
    public Cuota Cuota { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Monto { get; set; }
    public string MedioPago { get; set; } = "";

    // Identificador de la transacción que devuelve la pasarela.
    public string Referencia { get; set; } = "";
    public EstadoPago Estado { get; set; }

    // Solo los pagos aprobados tienen comprobante.
    public string? NumeroComprobante { get; set; }
}