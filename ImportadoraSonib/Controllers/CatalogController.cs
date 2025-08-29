using ImportadoraSonib.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers;

public class CatalogController : Controller
{
    private readonly ApplicationDbContext _db;
    public CatalogController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? category, int page = 1)
    {
        HttpContext.Session.SetString("lastCategory", category ?? "");
        var q = _db.Products.Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category!.Slug == category);

        var items = await q.OrderByDescending(p => p.CreatedAt)
                           .Take(24).ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Detail(string slug)
    {
        var p = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Slug == slug);
        if (p is null) return NotFound();
        return View(p);
    }
}
