using ImportadoraSonib.Data;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class CartItemsController : ControllerBase
{
    private readonly CartService _cart;
    private readonly ApplicationDbContext _db;

    public CartItemsController(CartService cart, ApplicationDbContext db)
    {
        _cart = cart; _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _cart.GetCurrentAsync();
        var result = items.Select(i => new {
            i.Id,
            i.ProductId,
            ProductName = i.Product?.Name ?? "(No disponible)",
            UnitPrice = i.Product?.Price ?? 0m,
            i.Quantity,
            Subtotal = (i.Product?.Price ?? 0m) * i.Quantity,
            i.CreatedAt
        }).ToList();

        var total = result.Sum(r => r.Subtotal);
        return Ok(new { total, items = result });
    }

    public record AddReq(int ProductId, int Qty);

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddReq req)
    {
        if (req.Qty <= 0) return BadRequest("Cantidad inválida.");
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.ProductId);
        if (p is null) return NotFound("Producto no existe o no disponible.");

        await _cart.AddAsync(req.ProductId, req.Qty);
        return NoContent();
    }

    // Quitar UN producto del carrito
    // DELETE /api/cartitems/{productId}
    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cart.RemoveAsync(productId);
        return NoContent();
    }

    // Vaciar TODO el carrito (usuario o sesión actual)
    // DELETE /api/cartitems
    [HttpDelete("all")]
    public async Task<IActionResult> ClearAll()
    {
        await _cart.ClearAsync();
        return NoContent();
    }
}
