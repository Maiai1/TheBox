namespace TheBox.Api.Domain;

// Guarda el hash de cada contraseña que tuvo el asegurado (incluida la provisoria).
public class PasswordHistorial
{
    public int Id { get; set; }
    public int AseguradoId { get; set; }
    public string PasswordHash { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
