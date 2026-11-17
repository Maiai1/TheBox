using System.Security.Claims;
using TheBox.Api.Dtos;
using TheBox.Api.Services;
using Microsoft.EntityFrameworkCore;
using TheBox.Api.Data;

namespace TheBox.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/login", LoginAsync);
        auth.MapPost("/cambiar-password", CambiarPasswordAsync).RequireAuthorization();

        // Ejemplo de endpoint del portal: bloqueado hasta que el asegurado cambie la contraseña provisoria.
        app.MapGet("/api/portal/me", PerfilAsync).RequireAuthorization("PasswordAlDia");
    }

    private static async Task<IResult> LoginAsync(LoginRequest req, AuthService svc, CancellationToken ct)
    {
        var r = await svc.LoginAsync(req, ct);
        if (!r.Ok)
            return Results.Json(new { errores = r.Errores }, statusCode: r.Status);

        return Results.Ok(r.Valor);
    }

    private static async Task<IResult> CambiarPasswordAsync(
        CambiarPasswordRequest req, ClaimsPrincipal user, AuthService svc, CancellationToken ct)
    {
        if (!int.TryParse(user.FindFirst("sub")?.Value, out var aseguradoId))
            return Results.Unauthorized();

        var r = await svc.CambiarPasswordAsync(aseguradoId, req, ct);
        if (!r.Ok)
            return Results.Json(new { errores = r.Errores }, statusCode: r.Status);

        return Results.Ok(r.Valor);
    }

    private static async Task<IResult> PerfilAsync(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!int.TryParse(user.FindFirst("sub")?.Value, out var aseguradoId))
            return Results.Unauthorized();

        var perfil = await db.Asegurados
            .Where(a => a.Id == aseguradoId)
            .Select(a => new PerfilResponse(a.Nombre, a.Dni, a.Email, a.Telefono))
            .FirstOrDefaultAsync(ct);

        if (perfil is null)
            return Results.NotFound();

        return Results.Ok(perfil);
    }
}
