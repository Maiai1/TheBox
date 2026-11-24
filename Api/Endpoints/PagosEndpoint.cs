using System.Security.Claims;
using TheBox.Api.Dtos;
using TheBox.Api.Services;

namespace TheBox.Api.Endpoints;

public sealed record PagarRequest(string? MedioPagoId);

public static class PagosEndpoints
{
    public static void MapPagosEndpoints(this WebApplication app)
    {
        // Solo asegurados con sesión iniciada y la contraseña provisoria ya cambiada.
        var api = app.MapGroup("/api").RequireAuthorization("PasswordAlDia");

        api.MapGet("/polizas/{polizaId:int}/cuotas", CuotasAsync);
        api.MapGet("/pagos/medios", () => Results.Ok(PagosService.Medios));
        api.MapGet("/pagos/proximo", ProximoAsync);
        api.MapPost("/cuotas/{cuotaId:int}/pagos", PagarAsync);
        api.MapGet("/pagos/{pagoId:int}/comprobante", ComprobanteAsync);
    }

    private static async Task<IResult> CuotasAsync(
        int polizaId, ClaimsPrincipal user, PagosService svc, CancellationToken ct)
    {
        if (!TryAseguradoId(user, out var aseguradoId))
            return Results.Unauthorized();

        return Responder(await svc.CuotasAsync(aseguradoId, polizaId, ct));
    }

    private static async Task<IResult> ProximoAsync(ClaimsPrincipal user, PagosService svc, CancellationToken ct)
    {
        if (!TryAseguradoId(user, out var aseguradoId))
            return Results.Unauthorized();

        var proximo = await svc.ProximoPagoAsync(aseguradoId, ct);
        if (proximo is null)
            return Results.NoContent();

        return Results.Ok(proximo);
    }

    private static async Task<IResult> PagarAsync(
        int cuotaId, PagarRequest req, ClaimsPrincipal user, PagosService svc, CancellationToken ct)
    {
        if (!TryAseguradoId(user, out var aseguradoId))
            return Results.Unauthorized();

        return Responder(await svc.PagarAsync(aseguradoId, cuotaId, req.MedioPagoId, ct));
    }

    private static async Task<IResult> ComprobanteAsync(
        int pagoId, ClaimsPrincipal user, PagosService svc, CancellationToken ct)
    {
        if (!TryAseguradoId(user, out var aseguradoId))
            return Results.Unauthorized();

        return Responder(await svc.ComprobanteAsync(aseguradoId, pagoId, ct));
    }

    private static bool TryAseguradoId(ClaimsPrincipal user, out int aseguradoId) =>
        int.TryParse(user.FindFirst("sub")?.Value, out aseguradoId);

    private static IResult Responder<T>(Resultado<T> r)
    {
        if (!r.Ok)
            return Results.Json(new { errores = r.Errores }, statusCode: r.Status);

        return Results.Ok(r.Valor);
    }
}
