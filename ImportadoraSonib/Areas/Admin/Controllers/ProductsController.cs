using ImportadoraSonib.Data;
using ImportadoraSonib.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize] // opcional: [Authorize(Roles="Admin")]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProductsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // Ignora el filtro global para ver también eliminados
        var list = await _db.Products
            .IgnoreQueryFilters()
            .Include(p => p.Category)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
        return View(new Product { IsActive = true, Stock = 0, Price = 0M });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product m)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", m.CategoryId);
            return View(m);
        }
        m.Slug = string.IsNullOrWhiteSpace(m.Slug)
            ? m.Name.Trim().ToLower().Replace(" ", "-")
            : m.Slug.Trim().ToLower().Replace(" ", "-");

        _db.Products.Add(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", p.CategoryId);
        return View(p);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Product form)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        // Concurrencia: compara RowVersion
        _db.Entry(p).Property("RowVersion").OriginalValue = form.RowVersion;

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", form.CategoryId);
            return View(form);
        }

        p.Name = form.Name;
        p.Slug = string.IsNullOrWhiteSpace(form.Slug)
            ? form.Name.Trim().ToLower().Replace(" ", "-")
            : form.Slug.Trim().ToLower().Replace(" ", "-");
        p.Description = form.Description;
        p.Price = form.Price;
        p.ImageUrl = form.ImageUrl;
        p.Tags = form.Tags ?? "";
        p.Stock = form.Stock;
        p.CategoryId = form.CategoryId;
        p.IsActive = form.IsActive;
        p.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
            TempData["ok"] = "Producto actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Otro usuario ya modificó este registro. Recarga la página.");
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", form.CategoryId);
            return View(form);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return View(p);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, byte[] rowVersion)
    {
        var p = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        // Concurrencia en Delete
        _db.Entry(p).Property("RowVersion").OriginalValue = rowVersion;

        // Soft delete
        p.IsDeleted = true;
        p.IsActive = false;

        try
        {
            await _db.SaveChangesAsync();
            TempData["ok"] = "Producto eliminado (soft delete).";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Concurrencia detectada al eliminar.");
            return View(p);
        }
    }
}
