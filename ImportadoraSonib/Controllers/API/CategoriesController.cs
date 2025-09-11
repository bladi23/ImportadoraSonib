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

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.Categories.Where(c=>c.IsActive)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync());

    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CategoryCreateDto dto)
    {
        var exists = await _db.Categories.AnyAsync(c => c.Slug == dto.Slug);
        if (exists) return Conflict("Slug ya existe.");
        var c = new Domain.Entities.Category { Name=dto.Name, Slug=dto.Slug, IsActive=dto.IsActive };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, new CategoryDto(c.Id, c.Name, c.Slug));
    }
}
