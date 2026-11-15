using Microsoft.EntityFrameworkCore;
using TheBox.Api.Domain;

namespace TheBox.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asegurado> Asegurados => Set<Asegurado>();
    public DbSet<PasswordHistorial> PasswordHistorial => Set<PasswordHistorial>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Asegurado>(e =>
        {
            e.Property(a => a.Dni).HasMaxLength(12).IsRequired();
            e.HasIndex(a => a.Dni).IsUnique();
            e.Property(a => a.Nombre).HasMaxLength(120).IsRequired();
            e.Property(a => a.PasswordHash).HasMaxLength(100).IsRequired();
            e.HasMany(a => a.Historial).WithOne().HasForeignKey(h => h.AseguradoId);
        });

        b.Entity<PasswordHistorial>(e =>
        {
            e.Property(h => h.PasswordHash).HasMaxLength(100).IsRequired();
        });
    }
}
