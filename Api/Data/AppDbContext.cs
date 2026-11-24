using Microsoft.EntityFrameworkCore;
using TheBox.Api.Domain;

namespace TheBox.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asegurado> Asegurados => Set<Asegurado>();
    public DbSet<PasswordHistorial> PasswordHistorial => Set<PasswordHistorial>();
    public DbSet<SolicitudPresupuesto> Solicitudes => Set<SolicitudPresupuesto>();
    public DbSet<FotoSolicitud> FotosSolicitud => Set<FotoSolicitud>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();
    public DbSet<Pago> Pagos => Set<Pago>();

    public DbSet<Poliza> Polizas => Set<Poliza>();


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

        b.Entity<SolicitudPresupuesto>(e =>
        {
            e.Property(s => s.Nombre).HasMaxLength(120).IsRequired();
            e.Property(s => s.Dni).HasMaxLength(12).IsRequired();
            e.Property(s => s.Email).HasMaxLength(160).IsRequired();
            e.Property(s => s.Telefono).HasMaxLength(30).IsRequired();
            e.Property(s => s.Marca).HasMaxLength(60).IsRequired();
            e.Property(s => s.Modelo).HasMaxLength(80).IsRequired();
            e.Property(s => s.Patente).HasMaxLength(10).IsRequired();
            e.Property(s => s.Cobertura).HasMaxLength(30).IsRequired();
            e.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(s => s.Estado);
            e.HasMany(s => s.Fotos).WithOne().HasForeignKey(f => f.SolicitudPresupuestoId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<FotoSolicitud>(e =>
        {
            e.Property(f => f.NombreOriginal).HasMaxLength(200).IsRequired();
            e.Property(f => f.RutaRelativa).HasMaxLength(200).IsRequired();
            e.Property(f => f.ContentType).HasMaxLength(50).IsRequired();
        });

        b.Entity<Poliza>(e =>
        {
            e.Property(p => p.Numero).HasMaxLength(20).IsRequired();
            e.HasIndex(p => p.Numero).IsUnique();
            e.Property(p => p.Marca).HasMaxLength(60).IsRequired();
            e.Property(p => p.Modelo).HasMaxLength(80).IsRequired();
            e.Property(p => p.Patente).HasMaxLength(10).IsRequired();
            e.Property(p => p.TipoSeguro).HasMaxLength(30).IsRequired();
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.PrimaMensual).HasPrecision(12, 2);
            e.HasIndex(p => p.AseguradoId);
            e.HasOne<Asegurado>().WithMany().HasForeignKey(p => p.AseguradoId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Cuota>(e =>
        {
            e.Property(c => c.Monto).HasPrecision(12, 2);
            e.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(c => new { c.PolizaId, c.Numero }).IsUnique();
            e.HasOne(c => c.Poliza).WithMany(p => p.Cuotas).HasForeignKey(c => c.PolizaId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Pago>(e =>
        {
            e.Property(p => p.Monto).HasPrecision(12, 2);
            e.Property(p => p.MedioPago).HasMaxLength(60).IsRequired();
            e.Property(p => p.Referencia).HasMaxLength(60).IsRequired();
            e.Property(p => p.NumeroComprobante).HasMaxLength(20);
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasOne(p => p.Cuota).WithMany().HasForeignKey(p => p.CuotaId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
