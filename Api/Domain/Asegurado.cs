namespace TheBox.Api.Domain;

public class Asegurado
{
    public int Id { get; set; }
    public string Dni { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool Activa { get; set; } = true;

    // true mientras tenga la contraseña provisoria genérica. Se apaga al cambiarla.
    public bool DebeCambiarPassword { get; set; } = true;

    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }

    public List<PasswordHistorial> Historial { get; set; } = [];
}
