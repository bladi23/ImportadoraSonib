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
{
    private readonly ApplicationDbContext _db;
    private readonly WhatsappLinkService _wa;
    private readonly RecoEventService _reco;


    public OrdersController(ApplicationDbContext db, WhatsappLinkService wa, RecoEventService reco)
    {
        _db = db; _wa = wa; _reco = reco;
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

        // Validar stock (si usas)
        foreach (var line in order.Items)
        {
            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == line.ProductId);
            if (p is null || !p.IsActive || p.IsDeleted)
                return Conflict(new { message = $"El producto {line.ProductId} ya no está disponible." });

            if (p.Stock < line.Quantity)
                return Conflict(new { message = $"Stock insuficiente para {p.Name}. Disponible: {p.Stock}, solicitado: {line.Quantity}." });
                
        }

        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var lines = order.Items.Join(prods, d => d.ProductId, p => p.Id, (d, p) => (p.Name, d.Quantity, d.UnitPrice));
        var link = _wa.BuildOrderLink(order.Id, lines.Select(x => (x.Name, x.Quantity, x.UnitPrice)), order.Total);
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
var sid = HttpContext.Session?.Id ?? Guid.NewGuid().ToString("N");

foreach (var od in order.Items)
{
    if (od.ProductId.HasValue) // solo si hay FK válida
        _ = _reco.TrackAsync("purchase", od.ProductId.Value, uid, sid);
}
        return Ok(new { orderId = order.Id, total = order.Total, whatsappUrl = link });
    }
   [Authorize]
[HttpGet("my")]
public async Task<IActionResult> MyOrders()
{
    var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var orders = await _db.Orders
        .AsNoTracking()
        .Where(o => o.UserId == uid)
        .OrderByDescending(o => o.Id)
        .Select(o => new { o.Id, o.Status, o.Total, o.OrderDate })
        .ToListAsync();

    return Ok(orders);
}

[Authorize]
[HttpGet("{id:int}")]
public async Task<IActionResult> GetById(int id)
{
    var o = await _db.Orders
        .Include(x => x.Items).ThenInclude(i => i.Product)
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id);

    if (o is null) return NotFound();

    var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (o.UserId != uid && !User.IsInRole("Admin")) return Forbid();

    return Ok(new
    {
        o.Id, o.Status, o.Total, o.OrderDate, o.PaidAt,
        Items = o.Items.Select(i => new
        {
            i.ProductId,
            Product = i.Product!.Name,
            i.Quantity,
            i.UnitPrice,
            Subtotal = i.UnitPrice * i.Quantity
        })
    });
}
}
