using ImportadoraSonib.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ProductsController(ApplicationDbContext db) => _db = db;

    // GET /api/products?category=tecnologia&page=1&pageSize=24&search=...
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 24, [FromQuery] string? search = null)
    {
        HttpContext.Session.SetString("lastCategory", category ?? "");

        var q = _db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category!.Slug == category);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Tags.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .Select(p => new {
                               p.Id, p.Name, p.Slug, p.Price, p.ImageUrl, p.Stock,
                               Category = p.Category!.Name
                           })
                           .ToListAsync();

        return Ok(new { total, items });
    }

    // GET /api/products/by-slug/{slug}
    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var p = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Slug == slug);
        if (p is null) return NotFound();
        return Ok(new {
            p.Id, p.Name, p.Slug, p.Description, p.Price, p.ImageUrl, p.Stock,
            Category = p.Category!.Name, p.CategoryId
        });
    }

    // GET /api/products/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return Ok(new {
            p.Id, p.Name, p.Slug, p.Description, p.Price, p.ImageUrl, p.Stock,
            Category = p.Category!.Name, p.CategoryId
        });
    }
}
