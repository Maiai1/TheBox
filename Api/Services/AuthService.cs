using Microsoft.EntityFrameworkCore;
using TheBox.Api.Data;
using TheBox.Api.Domain;
using TheBox.Api.Dtos;

namespace TheBox.Api.Services;

public sealed class AuthService(AppDbContext db, TokenService tokens)
{
    private const int MaxIntentos = 5;
    private const int MinutosBloqueo = 15;
    private const int CantidadHistorial = 3;

    // Se usa para que el tiempo de respuesta no delate si el DNI existe o no.
    private static readonly string HashFicticio = BCrypt.Net.BCrypt.HashPassword("no-existe");

    public async Task<Resultado<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        const string credencialesInvalidas = "DNI o contraseña incorrectos.";

        var dni = new string((req.Dni ?? "").Where(char.IsDigit).ToArray());
        if (dni.Length == 0 || string.IsNullOrEmpty(req.Password))
            return Resultado<LoginResponse>.Falla(StatusCodes.Status400BadRequest, "Ingresá tu DNI y tu contraseña.");

        var asegurado = await db.Asegurados.FirstOrDefaultAsync(a => a.Dni == dni, ct);
        if (asegurado is null)
        {
            BCrypt.Net.BCrypt.Verify(req.Password, HashFicticio);
            return Resultado<LoginResponse>.Falla(StatusCodes.Status401Unauthorized, credencialesInvalidas);
        }

        if (asegurado.BloqueadoHasta is { } hasta && hasta > DateTime.UtcNow)
            return Resultado<LoginResponse>.Falla(StatusCodes.Status429TooManyRequests,
                "Cuenta bloqueada temporalmente por intentos fallidos. Probá de nuevo en unos minutos.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, asegurado.PasswordHash))
        {
            asegurado.IntentosFallidos++;
            if (asegurado.IntentosFallidos >= MaxIntentos)
            {
                asegurado.BloqueadoHasta = DateTime.UtcNow.AddMinutes(MinutosBloqueo);
                asegurado.IntentosFallidos = 0;
            }
            await db.SaveChangesAsync(ct);
            return Resultado<LoginResponse>.Falla(StatusCodes.Status401Unauthorized, credencialesInvalidas);
        }

        asegurado.IntentosFallidos = 0;
        asegurado.BloqueadoHasta = null;
        await db.SaveChangesAsync(ct);

        var token = tokens.Crear(asegurado);
        return Resultado<LoginResponse>.Exito(new LoginResponse(token, asegurado.Nombre, asegurado.DebeCambiarPassword));
    }

    public async Task<Resultado<CambiarPasswordResponse>> CambiarPasswordAsync(
        int aseguradoId, CambiarPasswordRequest req, CancellationToken ct)
    {
        var asegurado = await db.Asegurados.FirstOrDefaultAsync(a => a.Id == aseguradoId, ct);
        if (asegurado is null)
            return Resultado<CambiarPasswordResponse>.Falla(StatusCodes.Status401Unauthorized, "Sesión inválida.");

        if (string.IsNullOrEmpty(req.PasswordActual) ||
            !BCrypt.Net.BCrypt.Verify(req.PasswordActual, asegurado.PasswordHash))
            return Resultado<CambiarPasswordResponse>.Falla(StatusCodes.Status400BadRequest,
                "La contraseña actual es incorrecta.");

        if (req.PasswordNueva != req.Confirmacion)
            return Resultado<CambiarPasswordResponse>.Falla(StatusCodes.Status400BadRequest,
                "Las contraseñas nuevas no coinciden.");

        var errores = PasswordPolicy.Validar(req.PasswordNueva);
        if (errores.Count > 0)
            return Resultado<CambiarPasswordResponse>.Falla(StatusCodes.Status400BadRequest, [.. errores]);

        var ultimas = await db.PasswordHistorial
            .Where(h => h.AseguradoId == aseguradoId)
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
            .Take(CantidadHistorial)
            .Select(h => h.PasswordHash)
            .ToListAsync(ct);

        if (ultimas.Any(hash => BCrypt.Net.BCrypt.Verify(req.PasswordNueva, hash)))
            return Resultado<CambiarPasswordResponse>.Falla(StatusCodes.Status400BadRequest,
                $"No puede ser igual a tus últimas {CantidadHistorial} contraseñas.");

        var nuevoHash = BCrypt.Net.BCrypt.HashPassword(req.PasswordNueva);
        asegurado.PasswordHash = nuevoHash;
        asegurado.DebeCambiarPassword = false;
        db.PasswordHistorial.Add(new PasswordHistorial { AseguradoId = asegurado.Id, PasswordHash = nuevoHash });
        await db.SaveChangesAsync(ct);

        // Token nuevo, ya sin la marca de "contraseña pendiente".
        return Resultado<CambiarPasswordResponse>.Exito(new CambiarPasswordResponse(tokens.Crear(asegurado)));
    }
}
