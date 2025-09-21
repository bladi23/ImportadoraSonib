using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) => _db = db;

    // Público: solo activas
    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.Categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.IsActive))
            .ToListAsync());

    // ───────────── ADMIN ─────────────

    // Listado completo (activas/inactivas)
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllAdmin() =>
        Ok(await _db.Categories
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.IsActive))
            .ToListAsync());

    // Crear
    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CategoryCreateDto dto)
    {
        var exists = await _db.Categories.AnyAsync(c => c.Slug == dto.Slug);
        if (exists) return Conflict("Slug ya existe.");

        var c = new Domain.Entities.Category { Name = dto.Name, Slug = dto.Slug, IsActive = dto.IsActive };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();

        var result = new CategoryDto(c.Id, c.Name, c.Slug, c.IsActive);
        return CreatedAtAction(nameof(GetAllAdmin), new { id = c.Id }, result);
    }

    // Actualizar
    [Authorize(Roles="Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryUpdateDto dto)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        // Si cambian el slug, valida unicidad
        if (!string.Equals(c.Slug, dto.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var slugTaken = await _db.Categories.AnyAsync(x => x.Slug == dto.Slug && x.Id != id);
            if (slugTaken) return Conflict("Slug ya existe.");
        }

        c.Name = dto.Name;
        c.Slug = dto.Slug;
        c.IsActive = dto.IsActive;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Borrar (si tiene productos, desactiva en lugar de borrar)
    [Authorize(Roles="Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        if (c.Products?.Any() == true)
        {
            c.IsActive = false;
            c.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { softDeleted = true, message = "Categoría con productos: se desactivó en lugar de borrar." });
        }

        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
