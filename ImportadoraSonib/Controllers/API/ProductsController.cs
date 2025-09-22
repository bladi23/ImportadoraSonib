using ImportadoraSonib.Data;
using ImportadoraSonib.Domain.Entities;
using ImportadoraSonib.DTOs;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Authorization; // [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly CatalogCacheStamp _stamp;

    public ProductsController(ApplicationDbContext db, IMemoryCache cache, CatalogCacheStamp stamp)
    {
        _db = db; _cache = cache; _stamp = stamp;
    }

    // =================== PUBLIC (con caché + stamp) ===================

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? category, [FromQuery] int page = 1,
                                         [FromQuery] int pageSize = 24, [FromQuery] string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        HttpContext.Session.SetString("lastCategory", category ?? "");

        var key = $"products:{category}:{page}:{pageSize}:{search}:{_stamp.Value}";
        if (_cache.TryGetValue(key, out object? cached) && cached is not null)
            return Ok(cached);

        var q = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category!.Slug == category);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Tags.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .Select(p => new ProductListItemDto(
                               p.Id, p.Name, p.Slug, p.Price, p.ImageUrl, p.Stock, p.Category!.Name))
                           .ToListAsync();

        var payload = new { total, page, pageSize, items };

        _cache.Set(key, payload, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        });

        return Ok(payload);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var key = $"product:slug:{slug}:{_stamp.Value}";
        if (_cache.TryGetValue(key, out object? cached) && cached is not null)
            return Ok(cached);

        var p = await _db.Products.AsNoTracking().Include(x => x.Category)
                                  .FirstOrDefaultAsync(x => x.Slug == slug);
        if (p is null) return NotFound();
        var dto = new ProductDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Price, p.ImageUrl, p.Stock, p.CategoryId, p.Category!.Name);

        _cache.Set(key, dto, TimeSpan.FromSeconds(60));
        return Ok(dto);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var key = $"product:id:{id}:{_stamp.Value}";
        if (_cache.TryGetValue(key, out object? cached) && cached is not null)
            return Ok(cached);

        var p = await _db.Products.AsNoTracking().Include(x => x.Category)
                                  .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var dto = new ProductDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Price, p.ImageUrl, p.Stock, p.CategoryId, p.Category!.Name);

        _cache.Set(key, dto, TimeSpan.FromSeconds(60));
        return Ok(dto);
    }

    // =================== ADMIN (concurrencia + invalidación) ===================

    private static string ToB64(byte[] rv) => Convert.ToBase64String(rv);
    private static byte[] FromB64(string s)
    {
        try { return Convert.FromBase64String(s); }
        catch { throw new ArgumentException("RowVersion inválida (no es Base64)."); }
    }

    // GET /api/products/admin?search=&page=1&pageSize=20&includeDeleted=true
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
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

        if (!includeDeleted)
            q = q.Where(p => !p.IsDeleted);

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

    // GET /api/products/admin/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/{id:int}")]
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

    // POST /api/products  (Admin create)
    [Authorize(Roles = "Admin")]
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

        // 🔔 invalidar catálogo para público
        _stamp.Bump();

        var res = new ProductAdminDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Tags, p.Price, p.ImageUrl,
            p.Stock, p.IsActive, p.CategoryId, (await _db.Categories.FindAsync(p.CategoryId))?.Name ?? "",
            ToB64(p.RowVersion));

        return CreatedAtAction(nameof(AdminGetById), new { id = p.Id }, res);
    }

    // PUT /api/products/{id} (Admin update con RowVersion)
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> AdminUpdate(int id, ProductUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (string.IsNullOrWhiteSpace(dto.RowVersion))
            return BadRequest("RowVersion requerida.");

        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        // Validar slug único si cambió
        if (!string.Equals(p.Slug, dto.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var slugTaken = await _db.Products.AnyAsync(x => x.Slug == dto.Slug && x.Id != id);
            if (slugTaken) return Conflict("Slug ya existe.");
        }

        // Concurrencia: establece la RowVersion ORIGINAL que envía el cliente
        try
        {
            _db.Entry(p).Property(x => x.RowVersion).OriginalValue = FromB64(dto.RowVersion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message); // Base64 inválido
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

            // 🔔 invalidar catálogo para público
            _stamp.Bump();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Recupera estado actual y devuelve 409 con nueva RowVersion
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

    // DELETE /api/products/{id} (soft delete)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> AdminDelete(int id)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        p.IsDeleted = true;
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 🔔 invalidar catálogo para público
        _stamp.Bump();

        return NoContent();
    }
}
