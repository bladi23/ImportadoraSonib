using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using ImportadoraSonib.Services;
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

    // ============ PÚBLICO (catálogo con caché + stamp) ============

    // GET /api/products?category=slug&page=1&pageSize=24&search=texto
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // (opcional) recuerda última categoría en sesión
        HttpContext.Session.SetString("lastCategory", category ?? "");

        // cache key incluye la “estampilla” para invalidación
        var key = $"products:{category}:{page}:{pageSize}:{search}:{_stamp.Value}";
        if (_cache.TryGetValue(key, out object? cached) && cached is not null)
            return Ok(cached);

        var q = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

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

    // GET /api/products/by-slug/{slug}
    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var key = $"product:slug:{slug}:{_stamp.Value}";
        if (_cache.TryGetValue(key, out object? cached) && cached is not null)
            return Ok(cached);

        var p = await _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (p is null) return NotFound();

        var dto = new ProductDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Price, p.ImageUrl, p.Stock, p.CategoryId, p.Category!.Name);

        _cache.Set(key, dto, TimeSpan.FromSeconds(60));
        return Ok(dto);
    }

    // GET /api/products/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var key = $"product:id:{id}:{_stamp.Value}";
        if (_cache.TryGetValue(key, out object? cached) && cached is not null)
            return Ok(cached);

        var p = await _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p is null) return NotFound();

        var dto = new ProductDetailDto(
            p.Id, p.Name, p.Slug, p.Description, p.Price, p.ImageUrl, p.Stock, p.CategoryId, p.Category!.Name);

        _cache.Set(key, dto, TimeSpan.FromSeconds(60));
        return Ok(dto);
    }
}
