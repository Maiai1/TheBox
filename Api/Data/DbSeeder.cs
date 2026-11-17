using Microsoft.EntityFrameworkCore;
using TheBox.Api.Domain;

namespace TheBox.Api.Data;

// Solo para desarrollo: crea la base y asegurados de prueba.
public static class DbSeeder
{
    public static async Task SembrarAsync(AppDbContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        await AgregarSiNoExisteAsync(db, "12345678901", new Asegurado
        {
            Dni = "38450123",
            Nombre = "Juan Pérez",
            Email = "juanperez@email.com",
            Telefono = "11 1234 5678",
            DebeCambiarPassword = true
        });

        await AgregarSiNoExisteAsync(db, "Maria2026", new Asegurado
        {
            Dni = "27999888",
            Nombre = "María González",
            Email = "maria.gonzalez@email.com",
            Telefono = "11 5555 0101",
            DebeCambiarPassword = false
        });

        await AgregarSiNoExisteAsync(db, "Carlos2026", new Asegurado
        {
            Dni = "30555444",
            Nombre = "Carlos Rodríguez",
            Email = "carlos.rodriguez@email.com",
            Telefono = "11 5555 0202",
            DebeCambiarPassword = false,
            Activa = false
        });

        await db.SaveChangesAsync();
    }

    private static async Task AgregarSiNoExisteAsync(AppDbContext db, string password, Asegurado asegurado)
    {
        if (await db.Asegurados.AnyAsync(a => a.Dni == asegurado.Dni))
            return;

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        asegurado.PasswordHash = hash;
        asegurado.Historial.Add(new PasswordHistorial { PasswordHash = hash });
        db.Asegurados.Add(asegurado);
    }
}