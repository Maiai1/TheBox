using Microsoft.EntityFrameworkCore;
using TheBox.Api.Data;
using TheBox.Api.Domain;
using TheBox.Api.Dtos;

namespace TheBox.Api.Services;

public sealed record MedioPago(string Id, string Descripcion);

public sealed record ResultadoPasarela(EstadoPago Estado, string Referencia, string Mensaje);

public sealed record CuotaDto(int Id, int Numero, DateOnly Vencimiento, decimal Monto, string Estado, int? PagoId, bool Pagable);

public sealed record CuotasPoliza(int PolizaId, string Numero, string EstadoPoliza, bool EnMora, List<CuotaDto> Cuotas);

public sealed record PagoRespuesta(
    string Estado, string Mensaje, string Referencia, int? PagoId, string? Comprobante,
    string EstadoCuota, string EstadoPoliza, bool EnMora);

public sealed record ComprobanteDto(
    string Numero, DateTime Fecha, string Asegurado, string Dni, string Poliza, string Vehiculo,
    int Cuota, DateOnly Vencimiento, decimal Monto, string MedioPago, string Referencia);

public sealed record ProximoPago(DateOnly Vencimiento, decimal Monto, string Poliza, bool Vencida);

// Contrato con la pasarela. Cuando se integre la real, se crea otra clase que implemente esta
// interfaz y se cambia el registro en Program.cs: el resto del sistema no se toca.
public interface IPasarelaPago
{
    Task<ResultadoPasarela> CobrarAsync(decimal monto, string medioPagoId, CancellationToken ct);
}

// Pasarela de prueba: decide el resultado según el medio elegido.
public sealed class PasarelaSimulada : IPasarelaPago
{
    public Task<ResultadoPasarela> CobrarAsync(decimal monto, string medioPagoId, CancellationToken ct)
    {
        var referencia = "SIM-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        var resultado = medioPagoId switch
        {
            "master-5100" => new ResultadoPasarela(EstadoPago.Rechazado, referencia,
                "La tarjeta fue rechazada por fondos insuficientes."),
            "pagofacil" => new ResultadoPasarela(EstadoPago.Pendiente, referencia,
                "Tu pago está pendiente de acreditación. Te avisaremos cuando se confirme."),
            _ => new ResultadoPasarela(EstadoPago.Aprobado, referencia,
                "Tu pago fue acreditado correctamente.")
        };

        return Task.FromResult(resultado);
    }
}

public sealed class PagosService(AppDbContext db, IPasarelaPago pasarela)
{
    // Medios de prueba. La aplicación nunca recibe números de tarjeta: solo el id del medio elegido.
    public static readonly MedioPago[] Medios =
    [
        new("visa-4242", "Visa terminada en 4242"),
        new("master-5100", "Mastercard terminada en 5100"),
        new("pagofacil", "Pago Fácil (efectivo)")
    ];

    public async Task<Resultado<CuotasPoliza>> CuotasAsync(int aseguradoId, int polizaId, CancellationToken ct)
    {
        var poliza = await db.Polizas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == polizaId && p.AseguradoId == aseguradoId, ct);
        if (poliza is null)
            return Resultado<CuotasPoliza>.Falla(StatusCodes.Status404NotFound, "La póliza no existe.");

        var hoy = Hoy();
        var cuotas = await db.Cuotas.AsNoTracking()
            .Where(c => c.PolizaId == polizaId)
            .OrderBy(c => c.Numero)
            .ToListAsync(ct);

        var ids = cuotas.Select(c => c.Id).ToList();
        var pagoPorCuota = (await db.Pagos.AsNoTracking()
                .Where(p => ids.Contains(p.CuotaId) && p.Estado == EstadoPago.Aprobado)
                .ToListAsync(ct))
            .GroupBy(p => p.CuotaId)
            .ToDictionary(g => g.Key, g => g.Max(p => p.Id));

        var dtos = cuotas.Select(c => new CuotaDto(
            c.Id, c.Numero, c.Vencimiento, c.Monto, EstadoVisible(c, hoy),
            pagoPorCuota.TryGetValue(c.Id, out var pagoId) ? pagoId : (int?)null,
            c.Estado == EstadoCuota.Pendiente && poliza.Estado != EstadoPoliza.Cancelada)).ToList();

        return Resultado<CuotasPoliza>.Exito(
            new CuotasPoliza(poliza.Id, poliza.Numero, poliza.Estado.ToString(), poliza.EnMora, dtos));
    }

    public async Task<Resultado<PagoRespuesta>> PagarAsync(
        int aseguradoId, int cuotaId, string? medioPagoId, CancellationToken ct)
    {
        var medio = Medios.FirstOrDefault(m => m.Id == medioPagoId);
        if (medio is null)
            return Resultado<PagoRespuesta>.Falla(StatusCodes.Status400BadRequest, "Elegí un medio de pago válido.");

        var cuota = await db.Cuotas.Include(c => c.Poliza)
            .FirstOrDefaultAsync(c => c.Id == cuotaId && c.Poliza.AseguradoId == aseguradoId, ct);
        if (cuota is null)
            return Resultado<PagoRespuesta>.Falla(StatusCodes.Status404NotFound, "La cuota no existe.");
        if (cuota.Estado == EstadoCuota.Pagada)
            return Resultado<PagoRespuesta>.Falla(StatusCodes.Status409Conflict, "La cuota ya está paga.");
        if (cuota.Poliza.Estado == EstadoPoliza.Cancelada)
            return Resultado<PagoRespuesta>.Falla(StatusCodes.Status409Conflict,
                "La póliza está cancelada y no admite pagos. Comunicate con atención al cliente.");

        var resultado = await pasarela.CobrarAsync(cuota.Monto, medio.Id, ct);
        var aprobado = resultado.Estado == EstadoPago.Aprobado;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var pago = new Pago
        {
            CuotaId = cuota.Id,
            Fecha = DateTime.UtcNow,
            Monto = cuota.Monto,
            MedioPago = medio.Descripcion,
            Referencia = resultado.Referencia,
            Estado = resultado.Estado
        };
        db.Pagos.Add(pago);

        // Rechazado o pendiente: la cuota conserva su estado impago original.
        if (aprobado)
        {
            cuota.Estado = EstadoCuota.Pagada;
            cuota.FechaPago = pago.Fecha;
        }
        await db.SaveChangesAsync(ct);

        if (aprobado)
        {
            pago.NumeroComprobante = $"CP-{pago.Id:D6}";
            await RecalcularMoraAsync(cuota.Poliza, ct);
            await db.SaveChangesAsync(ct);
        }

        await transaccion.CommitAsync(ct);

        return Resultado<PagoRespuesta>.Exito(new PagoRespuesta(
            resultado.Estado.ToString(), resultado.Mensaje, resultado.Referencia,
            aprobado ? pago.Id : null, pago.NumeroComprobante,
            EstadoVisible(cuota, Hoy()), cuota.Poliza.Estado.ToString(), cuota.Poliza.EnMora));
    }

    public async Task<Resultado<ComprobanteDto>> ComprobanteAsync(int aseguradoId, int pagoId, CancellationToken ct)
    {
        var pago = await db.Pagos.AsNoTracking()
            .Include(p => p.Cuota).ThenInclude(c => c.Poliza)
            .FirstOrDefaultAsync(p => p.Id == pagoId && p.Estado == EstadoPago.Aprobado
                                      && p.Cuota.Poliza.AseguradoId == aseguradoId, ct);
        if (pago is null)
            return Resultado<ComprobanteDto>.Falla(StatusCodes.Status404NotFound, "El comprobante no existe.");

        var asegurado = await db.Asegurados.AsNoTracking().FirstAsync(a => a.Id == aseguradoId, ct);
        var poliza = pago.Cuota.Poliza;

        return Resultado<ComprobanteDto>.Exito(new ComprobanteDto(
            pago.NumeroComprobante ?? $"CP-{pago.Id:D6}", pago.Fecha, asegurado.Nombre, asegurado.Dni,
            poliza.Numero, $"{poliza.Marca} {poliza.Modelo} {poliza.Anio}",
            pago.Cuota.Numero, pago.Cuota.Vencimiento, pago.Monto, pago.MedioPago, pago.Referencia));
    }

    // Próxima cuota a pagar (la más antigua sin pagar) entre todas las pólizas no canceladas.
    public async Task<ProximoPago?> ProximoPagoAsync(int aseguradoId, CancellationToken ct)
    {
        var proxima = await db.Cuotas.AsNoTracking()
            .Where(c => c.Poliza.AseguradoId == aseguradoId
                        && c.Estado == EstadoCuota.Pendiente
                        && c.Poliza.Estado != EstadoPoliza.Cancelada)
            .OrderBy(c => c.Vencimiento)
            .Select(c => new { c.Vencimiento, c.Monto, c.Poliza.Numero })
            .FirstOrDefaultAsync(ct);

        if (proxima is null)
            return null;

        return new ProximoPago(proxima.Vencimiento, proxima.Monto, proxima.Numero, proxima.Vencimiento < Hoy());
    }

    // Recalcula la morosidad. Si la póliza estaba suspendida por mora y ya no debe nada vencido, se reactiva.
    private async Task RecalcularMoraAsync(Poliza poliza, CancellationToken ct)
    {
        var hoy = Hoy();
        var teniaMora = poliza.EnMora;

        var tieneVencidas = await db.Cuotas.AnyAsync(
            c => c.PolizaId == poliza.Id && c.Estado == EstadoCuota.Pendiente && c.Vencimiento < hoy, ct);

        poliza.EnMora = tieneVencidas;

        if (teniaMora && !tieneVencidas && poliza.Estado == EstadoPoliza.Suspendida)
            poliza.Estado = EstadoPoliza.Vigente;
    }

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.Today);

    private static string EstadoVisible(Cuota cuota, DateOnly hoy)
    {
        if (cuota.Estado == EstadoCuota.Pagada)
            return "Pagada";
        if (cuota.Vencimiento < hoy)
            return "Vencida";

        return "Pendiente";
    }
}
