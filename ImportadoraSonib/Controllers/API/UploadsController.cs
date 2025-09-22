using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private const int MaxBytes = 5 * 1024 * 1024;       // 5 MB
    private const int MaxPixels = 5000;                 // protección contra “image bombs”
    private static readonly int[] Sizes = new[] {1600, 800, 400}; // xl, md, sm

    [Authorize(Roles = "Admin")]
    [HttpPost("product-image")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> UploadProductImage(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío.");
        if (file.Length > MaxBytes) return BadRequest("Tamaño máximo permitido: 5 MB.");

        // Validación básica de extensión; el procesamiento real valida el contenido
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext)) return BadRequest("Formato no permitido. Usa JPG, PNG o WEBP.");

        // Intentar abrir como imagen (valida que no sea contenido malicioso)
        using var stream = file.OpenReadStream();
        Image image;
        try
        {
            image = await Image.LoadAsync(stream, ct);
        }
        catch
        {
            return BadRequest("El archivo no es una imagen válida.");
        }

        // Protección por dimensiones (ej. 20000 x 20000)
        if (image.Width > MaxPixels || image.Height > MaxPixels)
            return BadRequest($"La imagen es demasiado grande (máximo {MaxPixels}px por lado).");

        // Carpeta destino
        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
        Directory.CreateDirectory(root);

        // Nombre base
        var baseName = Guid.NewGuid().ToString("N");
        var urls = new Dictionary<string, string>();

        // Generar 3 tamaños en WEBP (xl/md/sm) con calidad 75
        foreach (var target in Sizes)
        {
            // Clonar para no modificar el original entre iteraciones
            using var clone = image.Clone(x =>
            {
                x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(target, target)
                });
                // (Opcional) puedes agregar recorte centrado si tu catálogo lo requiere
            });

            var outName = $"{baseName}-{target}.webp";
            var outPath = Path.Combine(root, outName);

            // Guardar WEBP con calidad ~75 (bueno para catálogos)
            var encoder = new WebpEncoder { Quality = 75 };
            await clone.SaveAsWebpAsync(outPath, encoder, ct);

            var url = $"{Request.Scheme}://{Request.Host}/uploads/products/{outName}";
            urls[target == 1600 ? "xl" : target == 800 ? "md" : "sm"] = url;
        }

        // (Opcional) también puedes guardar el original como -orig.webp a calidad 85 si quieres
        // usando el mismo patrón.

        return Ok(new { ok = true, url = urls["md"], urls });
    }
}
