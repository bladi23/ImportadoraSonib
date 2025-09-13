using ImportadoraSonib.Data;
using ImportadoraSonib.DTOs;
using ImportadoraSonib.Services; // 👈
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
    private readonly CatalogCacheStamp _stamp; // 👈

    public AdminProductsController(ApplicationDbContext db, CatalogCacheStamp stamp) // 👈
    { _db = db; _stamp = stamp; }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        var slug = dto.Slug.Trim().ToLowerInvariant();
        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            return BadRequest("Categoría inválida.");
        if (await _db.Products.IgnoreQueryFilters().AnyAsync(p => p.Slug == slug))
            return Conflict("Slug ya existe.");

        var p = new Domain.Entities.Product
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim() ?? "",
            Price = dto.Price,
            ImageUrl = dto.ImageUrl?.Trim() ?? "",
            Stock = dto.Stock,
            CategoryId = dto.CategoryId,
            IsActive = dto.IsActive,
            IsDeleted = false
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        _stamp.Bump(); // 👈 invalidar caché

        return CreatedAtAction(nameof(Create), new { id = p.Id }, new { p.Id, p.Name, p.Slug });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
    {
        if (id != dto.Id) return BadRequest("Id inconsistente.");

        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        if (!dto.RowVersion.SequenceEqual(p.RowVersion))
            return Conflict("Conflicto de concurrencia. Refresca e intenta de nuevo.");

        var slug = dto.Slug.Trim().ToLowerInvariant();
        if (await _db.Products.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug && x.Id != id))
            return Conflict("Slug ya existe.");
        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            return BadRequest("Categoría inválida.");

        p.Name = dto.Name.Trim();
        p.Slug = slug;
        p.Description = dto.Description?.Trim() ?? "";
        p.Price = dto.Price;
        p.ImageUrl = dto.ImageUrl?.Trim() ?? "";
        p.Stock = dto.Stock;
        p.CategoryId = dto.CategoryId;
        p.IsActive = dto.IsActive;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _stamp.Bump(); // 👈 invalidar caché

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        p.IsDeleted = true;
        p.IsActive = false;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _stamp.Bump(); // 👈 invalidar caché

        return NoContent();
    }
}

