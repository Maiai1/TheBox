using System.Net.Mail;
using System.Text.RegularExpressions;
using TheBox.Api.Data;
using TheBox.Api.Domain;
using TheBox.Api.Dtos;

namespace TheBox.Api.Services;

public sealed record SolicitudCreada(int Id, string Estado);

public sealed class SolicitudService(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
{
    public const int MinFotos = 4;
    public const int MaxFotos = 6;
    public const long MaxBytesPorFoto = 4 * 1024 * 1024;

    private static readonly string[] Coberturas = ["Tercero Completo", "Responsabilidad Civil", "Todo Riesgo"];
    private static readonly string[] PreguntasDdjj = ["tieneGnc", "siniestros", "usoComercial"];
    private static readonly Regex PatenteRegex = new(@"^([A-Z]{3}[0-9]{3}|[A-Z]{2}[0-9]{3}[A-Z]{2})$", RegexOptions.Compiled);

    public async Task<Resultado<SolicitudCreada>> RegistrarAsync(IFormCollection form, CancellationToken ct)
    {
        var solicitud = LeerFormulario(form);
        var fotos = form.Files.GetFiles("fotos");

        var errores = ValidarDatos(solicitud, form);
        var (erroresFotos, extensiones) = await ValidarFotosAsync(fotos, ct);
        errores.AddRange(erroresFotos);

        if (errores.Count > 0)
            return Resultado<SolicitudCreada>.Falla(StatusCodes.Status400BadRequest, [.. errores]);

        var carpeta = Guid.NewGuid().ToString("N");
        var directorio = Path.Combine(RutaBase(), carpeta);
        Directory.CreateDirectory(directorio);

        try
        {
            for (var i = 0; i < fotos.Count; i++)
            {
                var nombreArchivo = $"{Guid.NewGuid():N}{extensiones[i]}";
                await using (var destino = File.Create(Path.Combine(directorio, nombreArchivo)))
                    await fotos[i].CopyToAsync(destino, ct);

                solicitud.Fotos.Add(new FotoSolicitud
                {
                    NombreOriginal = Truncar(Path.GetFileName(fotos[i].FileName), 200),
                    RutaRelativa = $"{carpeta}/{nombreArchivo}",
                    ContentType = Truncar(fotos[i].ContentType, 50),
                    Bytes = fotos[i].Length
                });
            }

            solicitud.DdjjFecha = DateTime.UtcNow;
            db.Solicitudes.Add(solicitud);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            Directory.Delete(directorio, recursive: true); // no dejar fotos huérfanas si algo falla
            throw;
        }

        return Resultado<SolicitudCreada>.Exito(new SolicitudCreada(solicitud.Id, solicitud.Estado.ToString()));
    }

    private string RutaBase() =>
        config["Almacenamiento:Solicitudes"] ?? Path.Combine(env.ContentRootPath, "App_Data", "solicitudes");

    private static SolicitudPresupuesto LeerFormulario(IFormCollection form)
    {
        _ = int.TryParse(Texto(form, "anio"), out var anio);

        return new SolicitudPresupuesto
        {
            Nombre = Texto(form, "nombre"),
            Dni = SoloDigitos(Texto(form, "dni")),
            Email = Texto(form, "email"),
            Telefono = Texto(form, "telefono"),
            Marca = Texto(form, "marca"),
            Modelo = Texto(form, "modelo"),
            Anio = anio,
            Patente = Texto(form, "patente").Replace(" ", "").Replace("-", "").ToUpperInvariant(),
            Cobertura = Texto(form, "cobertura"),
            DdjjAceptada = EsVerdadero(form, "ddjjAceptada"),
            TieneGnc = EsVerdadero(form, "tieneGnc"),
            SiniestrosUltimos3Anios = EsVerdadero(form, "siniestros"),
            UsoComercial = EsVerdadero(form, "usoComercial")
        };
    }

    private static List<string> ValidarDatos(SolicitudPresupuesto s, IFormCollection form)
    {
        var errores = new List<string>();

        if (s.Nombre.Length < 3)
            errores.Add("Ingresá tu nombre y apellido.");
        if (s.Dni.Length is < 7 or > 8)
            errores.Add("Ingresá un DNI válido (7 u 8 dígitos).");
        if (!EmailValido(s.Email))
            errores.Add("Ingresá un email válido.");
        if (SoloDigitos(s.Telefono).Length < 8)
            errores.Add("Ingresá un teléfono de contacto válido.");
        if (s.Marca.Length == 0)
            errores.Add("Ingresá la marca del vehículo.");
        if (s.Modelo.Length == 0)
            errores.Add("Ingresá el modelo del vehículo.");
        if (s.Anio < 1980 || s.Anio > DateTime.UtcNow.Year + 1)
            errores.Add("Ingresá un año de fabricación válido.");
        if (!PatenteRegex.IsMatch(s.Patente))
            errores.Add("Ingresá una patente válida (por ejemplo AB123CD o ABC123).");
        if (!Coberturas.Contains(s.Cobertura))
            errores.Add("Elegí una cobertura.");
        if (s.Nombre.Length > 120 || s.Marca.Length > 60 || s.Modelo.Length > 80 || s.Telefono.Length > 30)
            errores.Add("Alguno de los datos ingresados es demasiado largo.");

        // Declaración Jurada: todas las preguntas respondidas y aceptación expresa.
        if (PreguntasDdjj.Any(p => Texto(form, p).Length == 0))
            errores.Add("Respondé todas las preguntas de la Declaración Jurada.");
        if (!s.DdjjAceptada)
            errores.Add("Tenés que aceptar la Declaración Jurada para enviar la solicitud.");

        return errores;
    }

    private static async Task<(List<string> Errores, List<string> Extensiones)> ValidarFotosAsync(
        IReadOnlyList<IFormFile> fotos, CancellationToken ct)
    {
        var errores = new List<string>();
        var extensiones = new List<string>();

        if (fotos.Count < MinFotos)
            errores.Add($"Adjuntá al menos {MinFotos} fotografías del vehículo.");
        if (fotos.Count > MaxFotos)
            errores.Add($"Podés adjuntar hasta {MaxFotos} fotografías.");
        if (errores.Count > 0)
            return (errores, extensiones);

        foreach (var foto in fotos)
        {
            if (foto.Length is 0 or > MaxBytesPorFoto)
            {
                errores.Add("Cada fotografía debe pesar hasta 4 MB.");
                break;
            }

            var extension = await DetectarExtensionAsync(foto, ct);
            if (extension is null)
            {
                errores.Add("Las fotografías deben ser imágenes JPG, PNG o WEBP.");
                break;
            }

            extensiones.Add(extension);
        }

        return (errores, extensiones);
    }

    // Se mira el contenido real del archivo, no solo la extensión que declara el cliente.
    private static async Task<string?> DetectarExtensionAsync(IFormFile foto, CancellationToken ct)
    {
        var cabecera = new byte[12];
        await using var stream = foto.OpenReadStream();
        var leidos = await stream.ReadAsync(cabecera, ct);
        if (leidos < 12)
            return null;

        if (cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
            return ".jpg";
        if (cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47)
            return ".png";
        if (cabecera[0] == 'R' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == 'F' &&
            cabecera[8] == 'W' && cabecera[9] == 'E' && cabecera[10] == 'B' && cabecera[11] == 'P')
            return ".webp";

        return null;
    }

    private static string Texto(IFormCollection form, string clave) => form[clave].ToString().Trim();

    private static bool EsVerdadero(IFormCollection form, string clave) =>
        Texto(form, clave).Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string SoloDigitos(string valor) => new(valor.Where(char.IsDigit).ToArray());

    private static string Truncar(string valor, int max) => valor.Length <= max ? valor : valor[..max];

    private static bool EmailValido(string email) =>
        email.Length <= 160 && email.Contains('.') &&
        MailAddress.TryCreate(email, out var direccion) && direccion.Address == email;
}
