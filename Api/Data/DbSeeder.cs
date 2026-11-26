using Microsoft.EntityFrameworkCore;
using TheBox.Api.Domain;

namespace TheBox.Api.Data;

// Solo para desarrollo: crea la base y datos de prueba.
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
        await SembrarPolizasAsync(db);
        await SembrarCuotasAsync(db);
        await SembrarSiniestrosAsync(db);
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

    private static async Task SembrarPolizasAsync(AppDbContext db)
    {
        if (await db.Polizas.AnyAsync())
            return;

        var juan = await db.Asegurados.FirstAsync(a => a.Dni == "38450123");
        var maria = await db.Asegurados.FirstAsync(a => a.Dni == "27999888");

        db.Polizas.AddRange(
            Nueva(juan.Id, "POL-000123", "Toyota", "Corolla", 2022, "AB123CD", "Todo Riesgo",
                  EstadoPoliza.Vigente, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), 25000m, enMora: true),
            Nueva(juan.Id, "POL-000456", "Ford", "Ranger", 2021, "AA456BB", "Terceros Completo",
                  EstadoPoliza.Suspendida, new DateOnly(2023, 6, 1), new DateOnly(2024, 5, 31), 36200m, enMora: true, tuvoSuspension: true),
            Nueva(juan.Id, "POL-000789", "Honda", "Wave", 2020, "A123BCD", "Responsabilidad Civil",
                  EstadoPoliza.Cancelada, new DateOnly(2022, 3, 1), new DateOnly(2023, 2, 28), 9800m),

            Nueva(maria.Id, "POL-000321", "Volkswagen", "Gol Trend", 2019, "AC789DE", "Terceros Completo",
                  EstadoPoliza.Vigente, new DateOnly(2026, 3, 1), new DateOnly(2027, 2, 28), 27400m),
            Nueva(maria.Id, "POL-000654", "Fiat", "Cronos", 2021, "AD321FG", "Todo Riesgo",
                  EstadoPoliza.Suspendida, new DateOnly(2025, 8, 15), new DateOnly(2026, 8, 14), 41900m, enMora: true, tuvoSuspension: true));

        await db.SaveChangesAsync();
    }

    private static async Task SembrarSiniestrosAsync(AppDbContext db)
    {
        if (await db.Siniestros.AnyAsync())
            return;

        var polizas = await db.Polizas.ToDictionaryAsync(p => p.Numero);

        // Juan: tres siniestros, uno en análisis y dos cerrados.
        AgregarSiniestro(db, polizas["POL-000789"], new DateOnly(2023, 7, 5), "Accidente", "Av. Mitre 1500, Avellaneda",
            "Colisión con otro vehículo al girar en una esquina. Daños en el lateral derecho.", EstadoSiniestro.Cerrado);
        AgregarSiniestro(db, polizas["POL-000456"], new DateOnly(2023, 11, 20), "Robo", "Calle Belgrano 800, Lanús",
            "Robo del vehículo estacionado en la vía pública durante la noche.", EstadoSiniestro.Cerrado);
        AgregarSiniestro(db, polizas["POL-000123"], new DateOnly(2024, 3, 12), "Accidente", "Ruta 4 km 12, Lomas de Zamora",
            "Choque en cadena por frenada brusca. Daños en el paragolpes delantero y el capó.", EstadoSiniestro.EnAnalisis);

        // María: un siniestro en análisis.
        AgregarSiniestro(db, polizas["POL-000321"], DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), "Daños por granizo",
            "Estacionamiento del trabajo, Quilmes",
            "Granizo durante una tormenta. Abolladuras en el techo y el capó.", EstadoSiniestro.EnAnalisis);

        await db.SaveChangesAsync();
    }

    private static void AgregarSiniestro(
        AppDbContext db, Poliza poliza, DateOnly fecha, string tipo, string lugar, string descripcion, EstadoSiniestro estado)
    {
        var denuncia = fecha.ToDateTime(new TimeOnly(10, 0)).AddDays(1);

        var siniestro = new Siniestro
        {
            PolizaId = poliza.Id,
            Fecha = fecha,
            Tipo = tipo,
            Lugar = lugar,
            Descripcion = descripcion,
            Estado = estado,
            FechaDenuncia = denuncia
        };

        siniestro.Seguimiento.Add(new SeguimientoSiniestro
        {
            Fecha = denuncia,
            Estado = EstadoSiniestro.Recibido,
            Comentario = "Denuncia registrada por el asegurado."
        });
        siniestro.Seguimiento.Add(new SeguimientoSiniestro
        {
            Fecha = denuncia.AddDays(2),
            Estado = EstadoSiniestro.EnAnalisis,
            Comentario = "Un asesor está evaluando la documentación presentada."
        });
        if (estado == EstadoSiniestro.Cerrado)
        {
            siniestro.Seguimiento.Add(new SeguimientoSiniestro
            {
                Fecha = denuncia.AddDays(20),
                Estado = EstadoSiniestro.Cerrado,
                Comentario = "Siniestro resuelto y liquidado."
            });
        }

        db.Siniestros.Add(siniestro);
    }

    // Los vencimientos se arman relativos a hoy para que siempre haya cuotas vencidas y a vencer.
    private static async Task SembrarCuotasAsync(AppDbContext db)
    {
        if (await db.Cuotas.AnyAsync())
            return;

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var polizas = await db.Polizas.ToDictionaryAsync(p => p.Numero);

        // Juan: Corolla con 3 pagadas, 1 vencida y 2 a vencer.
        AgregarCuotas(db, polizas["POL-000123"], hoy, [-105, -75, -45, -15, 15, 45], pagadas: 3);
        // Juan: Ranger suspendida por mora (2 cuotas vencidas). Al pagarlas se reactiva.
        AgregarCuotas(db, polizas["POL-000456"], hoy, [-120, -90, -60, -30, 0, 30], pagadas: 2);
        // Juan: Wave cancelada, todo pago.
        AgregarCuotas(db, polizas["POL-000789"], hoy, [-420, -390, -360], pagadas: 3);

        // María: Gol Trend al día y Cronos suspendida por mora.
        AgregarCuotas(db, polizas["POL-000321"], hoy, [-62, -32, -2, 28, 58, 88], pagadas: 3);
        AgregarCuotas(db, polizas["POL-000654"], hoy, [-90, -60, -30, 0, 30], pagadas: 1);

        await db.SaveChangesAsync();

        // El número de comprobante se arma con el id del pago.
        foreach (var pago in await db.Pagos.Where(p => p.NumeroComprobante == null).ToListAsync())
            pago.NumeroComprobante = $"CP-{pago.Id:D6}";

        await db.SaveChangesAsync();
    }

    private static void AgregarCuotas(AppDbContext db, Poliza poliza, DateOnly hoy, int[] diasDesdeHoy, int pagadas)
    {
        for (var i = 0; i < diasDesdeHoy.Length; i++)
        {
            var cuota = new Cuota
            {
                PolizaId = poliza.Id,
                Numero = i + 1,
                Vencimiento = hoy.AddDays(diasDesdeHoy[i]),
                VencimientoOriginal = hoy.AddDays(diasDesdeHoy[i]),
                Monto = poliza.PrimaMensual
            };

            if (i < pagadas)
            {
                var fecha = cuota.Vencimiento.ToDateTime(new TimeOnly(15, 0)).AddDays(-1);
                cuota.Estado = EstadoCuota.Pagada;
                cuota.FechaPago = fecha;

                db.Pagos.Add(new Pago
                {
                    Cuota = cuota,
                    Fecha = fecha,
                    Monto = cuota.Monto,
                    MedioPago = "Visa terminada en 4242",
                    Referencia = $"SIM-SEED-{poliza.Numero}-{i + 1}",
                    Estado = EstadoPago.Aprobado
                });
            }

            db.Cuotas.Add(cuota);
        }
    }

    private static Poliza Nueva(
        int aseguradoId, string numero, string marca, string modelo, int anio, string patente,
        string tipoSeguro, EstadoPoliza estado, DateOnly desde, DateOnly hasta, decimal prima,
        bool enMora = false, bool tuvoSuspension = false) => new()
        {
            AseguradoId = aseguradoId,
            Numero = numero,
            Marca = marca,
            Modelo = modelo,
            Anio = anio,
            Patente = patente,
            TipoSeguro = tipoSeguro,
            Estado = estado,
            VigenciaDesde = desde,
            VigenciaHasta = hasta,
            PrimaMensual = prima,
            EnMora = enMora,
            TuvoSuspension = tuvoSuspension
        };
}