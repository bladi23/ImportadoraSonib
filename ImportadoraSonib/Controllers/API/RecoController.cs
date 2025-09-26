using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/reco")]
public class RecoController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public RecoController(ApplicationDbContext db) { _db = db; }

   [HttpGet("popular")]
public async Task<IActionResult> Popular([FromQuery] int take = 6, [FromQuery] int days = 90)
{
    take = Math.Clamp(take, 1, 24);
    var since = DateTime.UtcNow.AddDays(-Math.Abs(days));

    // 1) Conteo por producto (purchase=2, add=1) dentro de la ventana temporal
    var counts =
        from e in _db.ProductEvents.AsNoTracking()
        where e.CreatedAt >= since
        group e by e.ProductId into g
        select new
        {
            ProductId = g.Key,
            Score     = g.Sum(x => x.EventType == "purchase" ? 2 : 1),
            LastDate  = g.Max(x => x.CreatedAt)
        };

    // 2) Unir con productos activos y ordenar por score (y frescura)
    var query =
        from c in counts
        join p in _db.Products.AsNoTracking() on c.ProductId equals p.Id
        where p.IsActive && !p.IsDeleted
        orderby c.Score descending, c.LastDate descending
        select new ProductListItemDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Price,
            p.ImageUrl,
            p.Stock,
            p.Category != null ? p.Category.Name : "Otros" // coalesce seguro
        );

    var items = await query.Take(take).ToListAsync();
    return Ok(items);
}

    // GET api/reco/also-bought/7?take=6
    [HttpGet("also-bought/{productId:int}")]
    public async Task<IActionResult> AlsoBought(int productId, [FromQuery] int take = 6)
    {
        take = Math.Clamp(take, 1, 24);

        // sesiones/usuarios que compraron este producto
        var buyers =
            (from pe in _db.ProductEvents.AsNoTracking()
             where pe.EventType == "purchase" && pe.ProductId == productId
             select new { pe.UserId, pe.SessionId })
            .Distinct();

        // otros productos comprados por esos mismos buyers
        var candidateIds =
            from e in _db.ProductEvents.AsNoTracking()
            where e.EventType == "purchase"
               && e.ProductId != productId                         // 👈 sin null-check
            join b in buyers
              on new { e.UserId, e.SessionId } equals new { b.UserId, b.SessionId }
            group e by e.ProductId into g                           // 👈 clave int directa
            orderby g.Count() descending
            select g.Key;

        var ids = await candidateIds.Take(take * 2).ToListAsync();   // margen para filtrar
        ids = ids.Distinct().ToList();

        var cards = await _db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.IsActive && !p.IsDeleted)
            .Select(p => new
            {
                id = p.Id, name = p.Name, slug = p.Slug,
                price = p.Price, imageUrl = p.ImageUrl, stock = p.Stock,
                category = p.Category!.Name
            })
            .ToListAsync();

        // respeta el orden de 'ids'
        var result = ids
            .Select(id => cards.FirstOrDefault(c => c.id == id))
            .Where(c => c != null)
            .Take(take)
            .ToList();

        return Ok(result);
    }
// GET api/reco/popular-in-category/{categoryId}?take=6
[HttpGet("popular-in-category/{categoryId:int}")]
public async Task<IActionResult> PopularInCategory(int categoryId, [FromQuery] int take = 6)
{
    take = Math.Clamp(take, 1, 24);
    var since = DateTime.UtcNow.AddDays(-90);

    var counts =
        from e in _db.ProductEvents.AsNoTracking()
        where e.CreatedAt >= since
        group e by e.ProductId into g
        select new
        {
            ProductId = g.Key,
            Score     = g.Sum(x => x.EventType == "purchase" ? 2 : 1),
            LastDate  = g.Max(x => x.CreatedAt)
        };

    var ids = await (
        from c in counts
        join p in _db.Products.AsNoTracking() on c.ProductId equals p.Id
        where p.CategoryId == categoryId && p.IsActive && !p.IsDeleted
        orderby c.Score descending, c.LastDate descending
        select c.ProductId
    ).Take(take).ToListAsync();

    if (ids.Count == 0) return Ok(Array.Empty<object>());

    var cards = await _db.Products.AsNoTracking()
        .Where(p => ids.Contains(p.Id))
        .Select(p => new
        {
            id = p.Id,
            name = p.Name,
            slug = p.Slug,
            price = p.Price,
            imageUrl = p.ImageUrl,
            stock = p.Stock,
            category = p.Category != null ? p.Category.Name : "Otros"
        })
        .ToListAsync();

    var result = ids.Select(id => cards.First(c => c.id == id)).ToList();
    return Ok(result);
}


}
