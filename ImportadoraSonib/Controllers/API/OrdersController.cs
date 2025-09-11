using ImportadoraSonib.Data;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public OrdersController(ApplicationDbContext db) => _db = db;

    public record CartLine(int ProductId, int Quantity);
    public record CreateOrderReq(List<CartLine> Items);

    // POST /api/orders  (requiere login si quieres asociar al usuario)
    [Authorize] // quítalo si quieres permitir anónimo
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderReq req)
    {
        if (req.Items is null || req.Items.Count == 0) return BadRequest("Carrito vacío.");

        var ids = req.Items.Select(i => i.ProductId).ToList();
        var prods = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        if (prods.Count != ids.Count) return BadRequest("Producto inválido.");

        var userId = User?.Claims?.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;

        var order = new Domain.Entities.Order { UserId = userId, Status = "Pendiente" };
        foreach (var it in req.Items)
        {
            var p = prods.First(x => x.Id == it.ProductId);
            order.Items.Add(new Domain.Entities.OrderDetail
            {
                ProductId = p.Id,
                Quantity = it.Quantity,
                UnitPrice = p.Price
            });
        }
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var lines = order.Items.Join(prods, d => d.ProductId, p => p.Id, (d, p) => (p.Name, d.Quantity, d.UnitPrice));
        var link = WhatsappLinkService.BuildOrderLink(order.Id, lines, order.Total);

        return Ok(new { orderId = order.Id, total = order.Total, whatsappUrl = link });
    }
}
