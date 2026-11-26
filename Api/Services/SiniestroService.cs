using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TheBox.Api.Data;
using TheBox.Api.Domain;
using TheBox.Api.Dtos;

namespace TheBox.Api.Services;

public sealed record SiniestroResumen(int Id, string Numero, DateOnly Fecha, string Tipo, string Estado, string Poliza);

public sealed record SeguimientoDto(DateTime Fecha, string Estado, string Comentario);

public sealed record SiniestroDetalle(
    int Id, string Numero, DateOnly Fecha, string Tipo, string Estado, string Poliza, string Vehiculo,
    string Lugar, string Descripcion, DateTime FechaDenuncia, List<string> Documentos, List<SeguimientoDto> Seguimiento);

public sealed record SiniestroCreado(int Id, string Numero, string Estado);

public sealed class SiniestroService(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
{
    public const int MaxDocumentos = 5;
    public const long MaxBytesPorDocumento = 5 * 1024 * 1024;

    private static readonly string[] Tipos = ["Accidente", "Robo", "Incendio", "Daños por granizo", "Otro"];

    public async Task<List<SiniestroResumen>> ListarAsync(int aseguradoId, CancellationToken ct)
    {
        var filas = await db.Siniestros.AsNoTracking()
            .Where(s => s.Poliza.AseguradoId == aseguradoId)
            .OrderByDescending(s => s.Fecha).ThenByDescending(s => s.Id)
            .Select(s => new { s.Id, s.Fecha, s.Tipo, s.Estado, Poliza = s.Poliza.Numero })
            .ToListAsync(ct);

        return filas
            .Select(s => new SiniestroResumen(s.Id, Numero(s.Id), s.Fecha, s.Tipo, Etiqueta(s.Estado), s.Poliza))
            .ToList();
    }

    public async Task<Resultado<SiniestroDetalle>> DetalleAsync(int aseguradoId, int id, CancellationToken ct)
    {
        var s = await db.Siniestros.AsNoTracking()
            .Include(x => x.Poliza)
            .Include(x => x.Documentos)
            .Include(x => x.Seguimiento)
            .FirstOrDefaultAsync(x => x.Id == id && x.Poliza.AseguradoId == aseguradoId, ct);

        // Si es de otro asegurado también responde 404: no se revela que existe.
        if (s is null)
            return Resultado<SiniestroDetalle>.Falla(StatusCodes.Status404NotFound, "El siniestro no existe.");

        var seguimiento = s.Seguimiento
            .OrderBy(x => x.Fecha).ThenBy(x => x.Id)
            .Select(x => new SeguimientoDto(x.Fecha, Etiqueta(x.Estado), x.Comentario))
            .ToList();

        return Resultado<SiniestroDetalle>.Exito(new SiniestroDetalle(
            s.Id, Numero(s.Id), s.Fecha, s.Tipo, Etiqueta(s.Estado), s.Poliza.Numero,
            $"{s.Poliza.Marca} {s.Poliza.Modelo} {s.Poliza.Anio}", s.Lugar, s.Descripcion, s.FechaDenuncia,
            s.Documentos.Select(d => d.NombreOriginal).ToList(), seguimiento));
    }

    public async Task<Resultado<SiniestroCreado>> RegistrarAsync(int aseguradoId, IFormCollection form, CancellationToken ct)
    {
        var errores = new List<string>();

        _ = int.TryParse(Texto(form, "polizaId"), out var polizaId);
        var poliza = await db.Polizas.FirstOrDefaultAsync(p => p.Id == polizaId && p.AseguradoId == aseguradoId, ct);
        if (poliza is null)
            errores.Add("Elegí una póliza válida.");
        else if (poliza.Estado != EstadoPoliza.Vigente)
            errores.Add("Solo podés denunciar siniestros sobre pólizas vigentes.");

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        if (!DateOnly.TryParseExact(Texto(form, "fecha"), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fecha))
            errores.Add("Ingresá la fecha del siniestro.");
        else if (fecha > hoy)
            errores.Add("La fecha del siniestro no puede ser futura.");

        var tipo = Texto(form, "tipo");
        if (!Tipos.Contains(tipo))
            errores.Add("Elegí el tipo de siniestro.");

        var lugar = Texto(form, "lugar");
        if (lugar.Length is < 3 or > 200)
            errores.Add("Indicá el lugar donde ocurrió (hasta 200 caracteres).");

        var descripcion = Texto(form, "descripcion");
        if (descripcion.Length is < 20 or > 2000)
            errores.Add("Describí lo ocurrido (entre 20 y 2000 caracteres).");

        var documentos = form.Files.GetFiles("documentos");
        var (erroresDocs, extensiones) = await ValidarDocumentosAsync(documentos, ct);
        errores.AddRange(erroresDocs);

        if (errores.Count > 0)
            return Resultado<SiniestroCreado>.Falla(StatusCodes.Status400BadRequest, [.. errores]);

        var siniestro = new Siniestro
        {
            PolizaId = poliza!.Id,
            Fecha = fecha,
            Tipo = tipo,
            Lugar = lugar,
            Descripcion = descripcion,
            Estado = EstadoSiniestro.Recibido,
            FechaDenuncia = DateTime.UtcNow
        };
        siniestro.Seguimiento.Add(new SeguimientoSiniestro
        {
            Fecha = siniestro.FechaDenuncia,
            Estado = EstadoSiniestro.Recibido,
            Comentario = "Denuncia registrada por el asegurado."
        });

        var carpeta = Guid.NewGuid().ToString("N");
        var directorio = Path.Combine(RutaBase(), carpeta);
        Directory.CreateDirectory(directorio);

        try
        {
            for (var i = 0; i < documentos.Count; i++)
            {
                var nombreArchivo = $"{Guid.NewGuid():N}{extensiones[i]}";
                await using (var destino = File.Create(Path.Combine(directorio, nombreArchivo)))
                    await documentos[i].CopyToAsync(destino, ct);

                siniestro.Documentos.Add(new DocumentoSiniestro
                {
                    NombreOriginal = Truncar(Path.GetFileName(documentos[i].FileName), 200),
                    RutaRelativa = $"{carpeta}/{nombreArchivo}",
                    ContentType = Truncar(documentos[i].ContentType, 50),
                    Bytes = documentos[i].Length
                });
            }

            db.Siniestros.Add(siniestro);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            Directory.Delete(directorio, recursive: true); // no dejar archivos huérfanos si algo falla
            throw;
        }

        return Resultado<SiniestroCreado>.Exito(
            new SiniestroCreado(siniestro.Id, Numero(siniestro.Id), Etiqueta(siniestro.Estado)));
    }

    private string RutaBase() =>
        config["Almacenamiento:Siniestros"] ?? Path.Combine(env.ContentRootPath, "App_Data", "siniestros");

    private static async Task<(List<string> Errores, List<string> Extensiones)> ValidarDocumentosAsync(
        IReadOnlyList<IFormFile> documentos, CancellationToken ct)
    {
        var errores = new List<string>();
        var extensiones = new List<string>();

        if (documentos.Count == 0)
            errores.Add("Adjuntá al menos un documento respaldatorio (fotos, denuncia policial, etc.).");
        if (documentos.Count > MaxDocumentos)
            errores.Add($"Podés adjuntar hasta {MaxDocumentos} documentos.");
        if (errores.Count > 0)
            return (errores, extensiones);

        foreach (var documento in documentos)
        {
            if (documento.Length is 0 or > MaxBytesPorDocumento)
            {
                errores.Add("Cada documento debe pesar hasta 5 MB.");
                break;
            }

            var extension = await DetectarExtensionAsync(documento, ct);
            if (extension is null)
            {
                errores.Add("Los documentos deben ser imágenes (JPG, PNG, WEBP) o archivos PDF.");
                break;
            }

            extensiones.Add(extension);
        }

        return (errores, extensiones);
    }

    // Se mira el contenido real del archivo, no solo la extensión que declara el cliente.
    private static async Task<string?> DetectarExtensionAsync(IFormFile archivo, CancellationToken ct)
    {
        var cabecera = new byte[12];
        await using var stream = archivo.OpenReadStream();
        var leidos = await stream.ReadAsync(cabecera, ct);
        if (leidos < 12)
            return null;

        if (cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
            return ".jpg";
        if (cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47)
            return ".png";
        if (cabecera[0] == '%' && cabecera[1] == 'P' && cabecera[2] == 'D' && cabecera[3] == 'F')
            return ".pdf";
        if (cabecera[0] == 'R' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == 'F' &&
            cabecera[8] == 'W' && cabecera[9] == 'E' && cabecera[10] == 'B' && cabecera[11] == 'P')
            return ".webp";

        return null;
    }

    private static string Numero(int id) => $"SIN-{id:D6}";

    private static string Etiqueta(EstadoSiniestro estado) => estado switch
    {
        EstadoSiniestro.EnAnalisis => "En análisis",
        EstadoSiniestro.Cerrado => "Cerrado",
        _ => "Recibido"
    };

    private static string Texto(IFormCollection form, string clave) => form[clave].ToString().Trim();

    private static string Truncar(string valor, int max) => valor.Length <= max ? valor : valor[..max];
}