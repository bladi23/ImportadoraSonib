using ImportadoraSonib.Data;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{private readonly ApplicationDbContext _db;
private readonly WhatsappLinkService _wa;

public OrdersController(ApplicationDbContext db, WhatsappLinkService wa)
{
    _db = db; _wa = wa;
}


    public record CartLine(int ProductId, int Quantity);
    public record CreateOrderReq(List<CartLine> Items);

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderReq req)
    {
        if (req.Items is null || req.Items.Count == 0) return BadRequest("Carrito vacío.");

        var ids = req.Items.Select(i => i.ProductId).ToList();
        var prods = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();
        if (prods.Count != ids.Count) return BadRequest("Producto inválido.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = new Domain.Entities.Order { UserId = userId, Status = "Pendiente" };
        foreach (var it in req.Items)
        {
            if (it.Quantity <= 0) return BadRequest("Cantidad inválida.");
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
        var link = _wa.BuildOrderLink(order.Id, lines.Select(x => (x.Name, x.Quantity, x.UnitPrice)), order.Total);
        return Ok(new { orderId = order.Id, total = order.Total, whatsappUrl = link });
    }
}
