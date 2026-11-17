using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TheBox.Api.Data;
using TheBox.Api.Endpoints;
using TheBox.Api.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5080");

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<TokenService>();

// El HTML se abre desde el disco (origen "null"), por eso en desarrollo se permite cualquier origen.
// En producción: .WithOrigins("https://tu-dominio").
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true,
            NameClaimType = "name",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PasswordAlDia", p => p
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => !ctx.User.HasClaim(TokenService.ClaimPasswordPendiente, "true")));

var app = builder.Build();
var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "TheBox Frontend");
var archivos = new PhysicalFileProvider(frontendPath);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = archivos, DefaultFileNames = ["LogIn.html"] });
app.UseStaticFiles(new StaticFileOptions { FileProvider = archivos });

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SembrarAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.Run();
