using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TheBox.Api.Domain;

namespace TheBox.Api.Services;

public sealed class TokenService(IConfiguration config)
{
    public const string ClaimPasswordPendiente = "pwd_pendiente";

    public string Crear(Asegurado asegurado)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim("sub", asegurado.Id.ToString()),
            new Claim("name", asegurado.Nombre),
            new Claim(ClaimPasswordPendiente, asegurado.DebeCambiarPassword ? "true" : "false")
        };

        var jwt = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(config["Jwt:Minutos"]!)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
