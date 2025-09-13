using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly CatalogCacheStamp _stamp;

    public CategoriesController(ApplicationDbContext db, IMemoryCache cache, CatalogCacheStamp stamp)
    {
        _db = db; _cache = cache; _stamp = stamp;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var key = $"categories:list:{_stamp.Value}";
        if (_cache.TryGetValue(key, out object? cached) && cached is not null)
            return Ok(cached);

        var data = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync();

        _cache.Set(key, data, TimeSpan.FromSeconds(60));
        return Ok(data);
    }
}
