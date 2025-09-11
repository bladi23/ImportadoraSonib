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

    // GET /api/cartitems
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _cart.GetCurrentAsync();
        var total = items.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);

        var result = items.Select(i => new {
            i.Id,
            i.ProductId,
            ProductName = i.Product!.Name,
            UnitPrice = i.Product.Price,
            i.Quantity,
            Subtotal = i.Product.Price * i.Quantity,
            i.CreatedAt
        });

        return Ok(new { total, items = result });
    }

    // POST /api/cartitems  { productId, qty }
    public record AddReq(int ProductId, int Qty);

    [HttpPost]
    public async Task<IActionResult> Add(AddReq req)
    {
        if (req.Qty <= 0) return BadRequest("Cantidad inválida.");
        var exists = await _db.Products.AnyAsync(p => p.Id == req.ProductId);
        if (!exists) return NotFound("Producto no existe.");

        await _cart.AddAsync(req.ProductId, req.Qty);
        return NoContent();
    }

    // DELETE /api/cartitems/{productId}
    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cart.RemoveAsync(productId);
        return NoContent();
    }
}
