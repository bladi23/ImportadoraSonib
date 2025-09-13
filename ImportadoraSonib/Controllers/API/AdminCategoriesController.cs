using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using ImportadoraSonib.Services; // 👈 importa
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly CatalogCacheStamp _stamp; // 👈 campo

    public AdminCategoriesController(ApplicationDbContext db, CatalogCacheStamp stamp) // 👈 inyecta
    {
        _db = db; _stamp = stamp;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto)
    {
        var slug = dto.Slug.Trim().ToLowerInvariant();
        if (await _db.Categories.AnyAsync(c => c.Slug == slug))
            return Conflict("Slug ya existe.");

        var c = new Domain.Entities.Category { Name = dto.Name.Trim(), Slug = slug, IsActive = dto.IsActive };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        _stamp.Bump(); // 👈 invalidar caché

        return CreatedAtAction(nameof(Create), new { id = c.Id }, new CategoryDto(c.Id, c.Name, c.Slug));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateDto dto)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        var newSlug = dto.Slug.Trim().ToLowerInvariant();
        var exists = await _db.Categories.AnyAsync(x => x.Slug == newSlug && x.Id != id);
        if (exists) return Conflict("Slug ya existe.");

        c.Name = dto.Name.Trim();
        c.Slug = newSlug;
        c.IsActive = dto.IsActive;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _stamp.Bump(); // 👈 invalidar caché

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        _stamp.Bump(); // 👈 invalidar caché

        return NoContent();
    }
}
