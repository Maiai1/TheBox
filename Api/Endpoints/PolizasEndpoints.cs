using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TheBox.Api.Data;

namespace TheBox.Api.Endpoints;

public sealed record PolizaResumen(
    int Id, string Numero, string Vehiculo, string TipoSeguro, string Estado,
    DateOnly VigenciaDesde, DateOnly VigenciaHasta);

public sealed record PolizaDetalle(
    int Id, string Numero, string Marca, string Modelo, int Anio, string Patente,
    string TipoSeguro, string Estado, DateOnly VigenciaDesde, DateOnly VigenciaHasta, decimal PrimaMensual);

public static class PolizasEndpoints
{
    public static void MapPolizasEndpoints(this WebApplication app)
    {
        // Solo asegurados con sesión iniciada y la contraseña provisoria ya cambiada.
        var grupo = app.MapGroup("/api/polizas").RequireAuthorization("PasswordAlDia");
        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", DetalleAsync);
    }

    private static async Task<IResult> ListarAsync(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!int.TryParse(user.FindFirst("sub")?.Value, out var aseguradoId))
            return Results.Unauthorized();

        var polizas = await db.Polizas
            .AsNoTracking()
            .Where(p => p.AseguradoId == aseguradoId)
            .OrderByDescending(p => p.VigenciaDesde)
            .ToListAsync(ct);

        var respuesta = polizas.Select(p => new PolizaResumen(
            p.Id, p.Numero, $"{p.Marca} {p.Modelo} {p.Anio}", p.TipoSeguro,
            p.Estado.ToString(), p.VigenciaDesde, p.VigenciaHasta));

        return Results.Ok(respuesta);
    }

    private static async Task<IResult> DetalleAsync(int id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!int.TryParse(user.FindFirst("sub")?.Value, out var aseguradoId))
            return Results.Unauthorized();

        var poliza = await db.Polizas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.AseguradoId == aseguradoId, ct);

        // Si la póliza es de otro asegurado también responde 404: no se revela que existe.
        if (poliza is null)
            return Results.NotFound();

        return Results.Ok(new PolizaDetalle(
            poliza.Id, poliza.Numero, poliza.Marca, poliza.Modelo, poliza.Anio, poliza.Patente,
            poliza.TipoSeguro, poliza.Estado.ToString(), poliza.VigenciaDesde, poliza.VigenciaHasta, poliza.PrimaMensual));
    }
}