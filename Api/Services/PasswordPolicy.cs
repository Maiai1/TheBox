namespace TheBox.Api.Services;

// Mismas reglas que se muestran en la pantalla "Cambiá tu contraseña".
public static class PasswordPolicy
{
    public const int LargoMinimo = 8;

    public static List<string> Validar(string? password)
    {
        var errores = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errores.Add($"Mínimo {LargoMinimo} caracteres.");
            errores.Add("Al menos una letra y un número.");
            return errores;
        }

        if (password.Length < LargoMinimo)
            errores.Add($"Mínimo {LargoMinimo} caracteres.");

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            errores.Add("Al menos una letra y un número.");

        return errores;
    }
}
