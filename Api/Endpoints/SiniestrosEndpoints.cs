using System.Security.Claims;
using TheBox.Api.Dtos;
using TheBox.Api.Services;

namespace TheBox.Api.Endpoints;

public static class SiniestrosEndpoints
{
    public static void MapSiniestrosEndpoints(this WebApplication app)
    {
        // Solo asegurados con sesión iniciada y la contraseña provisoria ya cambiada.
        var grupo = app.MapGroup("/api/siniestros").RequireAuthorization("PasswordAlDia");

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", DetalleAsync);
        grupo.MapPost("/", RegistrarAsync);
    }

    private static async Task<IResult> ListarAsync(ClaimsPrincipal user, SiniestroService svc, CancellationToken ct)
    {
        if (!TryAseguradoId(user, out var aseguradoId))
            return Results.Unauthorized();

        return Results.Ok(await svc.ListarAsync(aseguradoId, ct));
    }

    private static async Task<IResult> DetalleAsync(int id, ClaimsPrincipal user, SiniestroService svc, CancellationToken ct)
    {
        if (!TryAseguradoId(user, out var aseguradoId))
            return Results.Unauthorized();

        return Responder(await svc.DetalleAsync(aseguradoId, id, ct));
    }

    private static async Task<IResult> RegistrarAsync(
        HttpRequest request, ClaimsPrincipal user, SiniestroService svc, CancellationToken ct)
    {
        if (!TryAseguradoId(user, out var aseguradoId))
            return Results.Unauthorized();

        if (!request.HasFormContentType)
            return Results.Json(new { errores = new[] { "Formato de solicitud inválido." } },
                statusCode: StatusCodes.Status400BadRequest);

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(ct);
        }
        catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException)
        {
            return Results.Json(new { errores = new[] { "Los documentos superan el tamaño máximo permitido." } },
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        return Responder(await svc.RegistrarAsync(aseguradoId, form, ct));
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
