using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using ImportadoraSonib.Services;
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
    private readonly CatalogCacheStamp _stamp;

    public AdminCategoriesController(ApplicationDbContext db, CatalogCacheStamp stamp)
    {
        _db = db; _stamp = stamp;
    }

    // GET /api/admin/categories  (lista completa para admin, con conteo de productos)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new {
                c.Id, c.Name, c.Slug, c.IsActive,
                productCount = _db.Products.Count(p => p.CategoryId == c.Id && !p.IsDeleted)
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/admin/categories/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        return Ok(new CategoryDto(c.Id, c.Name, c.Slug));
    }

    // POST /api/admin/categories
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var slug = (dto.Slug ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest("Slug requerido.");

        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("Nombre requerido.");

        if (await _db.Categories.AnyAsync(c => c.Slug == slug))
            return Conflict("Slug ya existe.");

        var c = new Domain.Entities.Category { Name = name, Slug = slug, IsActive = dto.IsActive };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        _stamp.Bump(); // invalidar caché catálogo público

        // redirige al GET by id
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, new CategoryDto(c.Id, c.Name, c.Slug));
    }

    // PUT /api/admin/categories/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        var newSlug = (dto.Slug ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(newSlug)) return BadRequest("Slug requerido.");

        var newName = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Nombre requerido.");

        var exists = await _db.Categories.AnyAsync(x => x.Slug == newSlug && x.Id != id);
        if (exists) return Conflict("Slug ya existe.");

        c.Name = newName;
        c.Slug = newSlug;
        c.IsActive = dto.IsActive;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _stamp.Bump();

        return NoContent();
    }

    // DELETE /api/admin/categories/{id}
    // Bloquea si tiene productos (evita cascada)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted);
        if (hasProducts)
            return Conflict("No se puede eliminar: la categoría tiene productos. Mueve/borra los productos primero.");

        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        _stamp.Bump();

        return NoContent();
    }
}
