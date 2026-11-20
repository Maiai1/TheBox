using TheBox.Api.Services;

namespace TheBox.Api.Endpoints;

public static class PresupuestoEndpoints
{
    public static void MapPresupuestoEndpoints(this WebApplication app)
    {
        // Formulario público "No soy cliente": no requiere sesión.
        app.MapPost("/api/presupuestos", RegistrarAsync);
    }

    private static async Task<IResult> RegistrarAsync(HttpRequest request, SolicitudService svc, CancellationToken ct)
    {
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
            return Results.Json(new { errores = new[] { "Las fotografías superan el tamaño máximo permitido." } },
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        var r = await svc.RegistrarAsync(form, ct);
        if (!r.Ok)
            return Results.Json(new { errores = r.Errores }, statusCode: r.Status);

        return Results.Ok(r.Valor);
    }
}