namespace TheBox.Api.Dtos;

public sealed record LoginRequest(string? Dni, string? Password);

public sealed record LoginResponse(string Token, string Nombre, bool DebeCambiarPassword);

public sealed record CambiarPasswordRequest(string? PasswordActual, string? PasswordNueva, string? Confirmacion);

public sealed record CambiarPasswordResponse(string Token);

public sealed record PerfilResponse(string Nombre, string Dni, string? Email, string? Telefono);

public sealed record Resultado<T>(T? Valor, int Status, string[]? Errores)
{
    public bool Ok => Status == StatusCodes.Status200OK;

    public static Resultado<T> Exito(T valor) => new(valor, StatusCodes.Status200OK, null);

    public static Resultado<T> Falla(int status, params string[] errores) => new(default, status, errores);
}
