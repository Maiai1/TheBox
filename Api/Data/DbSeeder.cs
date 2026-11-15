using Microsoft.EntityFrameworkCore;
using TheBox.Api.Domain;

namespace TheBox.Api.Data;

// Solo para desarrollo: crea la base y un asegurado de prueba con contraseña provisoria.
public static class DbSeeder
{
    public const string PasswordProvisoria = "12345678901";

    public static async Task SembrarAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Asegurados.AnyAsync())
            return;

        var hash = BCrypt.Net.BCrypt.HashPassword(PasswordProvisoria);
        var asegurado = new Asegurado
        {
            Dni = "38450123",
            Nombre = "Juan Pérez",
            PasswordHash = hash,
            DebeCambiarPassword = true
        };
        asegurado.Historial.Add(new PasswordHistorial { PasswordHash = hash });

        db.Asegurados.Add(asegurado);
        await db.SaveChangesAsync();
    }
}
