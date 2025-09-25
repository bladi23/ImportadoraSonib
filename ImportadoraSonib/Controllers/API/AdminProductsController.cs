using System.Security.Claims;
using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly CatalogCacheStamp _stamp;
    private readonly IWebHostEnvironment _env;

    public AdminProductsController(ApplicationDbContext db, CatalogCacheStamp stamp, IWebHostEnvironment env)
    {
        _db = db; _stamp = stamp; _env = env;
    }

    private static string ToB64(byte[] rv) => Convert.ToBase64String(rv);
    private static byte[] FromB64(string s)
    {
        try { return Convert.FromBase64String(s); }
        catch { throw new ArgumentException("RowVersion inválida (no es Base64)."); }
    }

    // GET /api/admin/products?search=&page=1&pageSize=20&includeDeleted=true
    [HttpGet]
    public async Task<IActionResult> AdminList(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = true)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Products
            .IgnoreQueryFilters() // ver también inactivos/eliminados
            .Include(p => p.Category)
            .AsQueryable();

        if (!includeDeleted) q = q.Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Slug.Contains(search) || p.Tags.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductAdminListDto(
                p.Id, p.Name, p.Slug, p.Price, p.Stock, p.IsActive, p.IsDeleted, p.CategoryId, p.Category!.Name, ToB64(p.RowVersion)
            ))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/admin/products/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> AdminGetById(int id)
    {
        var p = await _db.Products.IgnoreQueryFilters()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        var dto = new ProductAdminDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Tags, p.Price, p.ImageUrl,
            p.Stock, p.IsActive, p.CategoryId, p.Category!.Name, ToB64(p.RowVersion));

        return Ok(dto);
    }

    // POST /api/admin/products  (crear)
    [HttpPost]
    public async Task<IActionResult> AdminCreate(ProductCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var slugTaken = await _db.Products.AnyAsync(x => x.Slug == dto.Slug);
        if (slugTaken) return Conflict("Slug ya existe.");

        var p = new Domain.Entities.Product
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description ?? "",
            Tags = dto.Tags ?? "",
            Price = dto.Price,
            ImageUrl = dto.ImageUrl ?? "",
            Stock = dto.Stock,
            IsActive = dto.IsActive,
            IsDeleted = false,
            CategoryId = dto.CategoryId
        };

        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        _stamp.Bump(); // invalida caché del catálogo

        var res = new ProductAdminDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Tags, p.Price, p.ImageUrl,
            p.Stock, p.IsActive, p.CategoryId, (await _db.Categories.FindAsync(p.CategoryId))?.Name ?? "",
            ToB64(p.RowVersion));

        return CreatedAtAction(nameof(AdminGetById), new { id = p.Id }, res);
    }

    // PUT /api/admin/products/{id}  (update con concurrencia)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> AdminUpdate(int id, ProductUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (string.IsNullOrWhiteSpace(dto.RowVersion))
            return BadRequest("RowVersion requerida.");

        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        if (!string.Equals(p.Slug, dto.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var slugTaken = await _db.Products.AnyAsync(x => x.Slug == dto.Slug && x.Id != id);
            if (slugTaken) return Conflict("Slug ya existe.");
        }

        try
        {
            _db.Entry(p).Property(x => x.RowVersion).OriginalValue = FromB64(dto.RowVersion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        p.Name = dto.Name;
        p.Slug = dto.Slug;
        p.Description = dto.Description ?? "";
        p.Tags = dto.Tags ?? "";
        p.Price = dto.Price;
        p.ImageUrl = dto.ImageUrl ?? "";
        p.Stock = dto.Stock;
        p.IsActive = dto.IsActive;
        p.CategoryId = dto.CategoryId;
        p.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
            _stamp.Bump();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            var current = await _db.Products.Include(x => x.Category).FirstAsync(x => x.Id == id);
            return Conflict(new
            {
                message = "Conflicto de concurrencia. Otro usuario modificó este producto.",
                current = new ProductAdminDetailDto(
                    current.Id, current.Name, current.Slug, current.Description, current.Tags, current.Price,
                    current.ImageUrl, current.Stock, current.IsActive, current.CategoryId, current.Category!.Name,
                    ToB64(current.RowVersion)
                )
            });
        }
    }

    // DELETE /api/admin/products/{id}  (soft delete)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> AdminDelete(int id)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        p.IsDeleted = true;
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _stamp.Bump();

        return NoContent();
    }

    // POST /api/admin/products/{id}/image  (subida de imagen)
    [HttpPost("{id:int}/image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound("Producto no existe.");
        if (file is null || file.Length == 0) return BadRequest("Archivo requerido.");

        if (file.Length > 2 * 1024 * 1024) return BadRequest("Máximo 2 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext)) return BadRequest("Formatos permitidos: jpg, jpeg, png, webp.");

        var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(root, "uploads", "products");
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(dir, name);
        using (var fs = System.IO.File.Create(path))
        {
            await file.CopyToAsync(fs);
        }

        var url = $"/uploads/products/{name}";
        p.ImageUrl = url;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _stamp.Bump();

        return Ok(new { imageUrl = url });
    }
}
