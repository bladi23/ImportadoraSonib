using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost("product-image")]
    [RequestSizeLimit(20_000_000)] // 20 MB
    public async Task<IActionResult> UploadProductImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Archivo vacío.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext)) return BadRequest("Formato no permitido.");

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var savePath = Path.Combine("wwwroot", "uploads", "products", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

        using (var fs = System.IO.File.Create(savePath))
            await file.CopyToAsync(fs);

        var publicUrl = $"{Request.Scheme}://{Request.Host}/uploads/products/{fileName}";
        return Ok(new { url = publicUrl });
    }
}
