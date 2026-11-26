namespace TheBox.Api.Domain;

// Aviso para el personal de la aseguradora (no es una notificación para el asegurado).
public class NotificacionInterna
{
    public int Id { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    // Ej: "CambioVencimiento".
    public string Tipo { get; set; } = "";
    public int? PolizaId { get; set; }
    public int? CuotaId { get; set; }
    public string Mensaje { get; set; } = "";
    public bool Leida { get; set; }
}
