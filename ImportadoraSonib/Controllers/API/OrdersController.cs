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
    if (req?.Items == null || req.Items.Count == 0)
        return BadRequest("Carrito vacío.");

    // 1) Normalizar: quitar cantidades no válidas y unir duplicados
    var wanted = req.Items
        .Where(i => i.ProductId > 0 && i.Quantity > 0)
        .GroupBy(i => i.ProductId)
        .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => Math.Max(1, x.Quantity)) })
        .ToList();

    if (wanted.Count == 0)
        return BadRequest("Carrito vacío.");

    var ids = wanted.Select(x => x.ProductId).ToList(); // ya están “distinct” por el GroupBy

    // 2) Traer productos existentes
    var prods = await _db.Products
        .AsNoTracking()
        .Where(p => ids.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id);

    if (prods.Count != ids.Count)
        return BadRequest("Producto inválido.");

    // 3) Validaciones de estado y stock
    foreach (var w in wanted)
    {
        var p = prods[w.ProductId];
        if (p.IsDeleted || !p.IsActive)
            return Conflict(new { message = $"El producto {p.Name} ya no está disponible." });

        if (p.Stock < w.Qty)
            return Conflict(new { message = $"Stock insuficiente para {p.Name}. Disponible: {p.Stock}, solicitado: {w.Qty}." });
    }

    // 4) Construir la orden
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var order = new Domain.Entities.Order
    {
        UserId    = userId,
        Status    = "Pendiente",
        OrderDate = DateTime.UtcNow
    };

    foreach (var w in wanted)
    {
        var p = prods[w.ProductId];
        order.Items.Add(new Domain.Entities.OrderDetail
        {
            ProductId = p.Id,
            Quantity  = w.Qty,
            UnitPrice = p.Price
        });
    }

    order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

    // 5) Guardar orden
    _db.Orders.Add(order);
    await _db.SaveChangesAsync();

    // 6) Link de WhatsApp
    var lines = order.Items.Select(d =>
    {
        var p = prods[d.ProductId!.Value];   // ProductId lo acabamos de asignar, existe
        return (p.Name, d.Quantity, d.UnitPrice);
    });
    var link = _wa.BuildOrderLink(order.Id, lines, order.Total);

    // 7) Registrar eventos de compra (¡sin fire-and-forget!)
    var sid = $"order-{order.Id}";
    try
    {
        // Opción A (recomendada): un solo roundtrip a BD
        await _reco.TrackManyAsync("purchase", ids, userId, sid);

        // Opción B (si no tienes TrackManyAsync):
        // foreach (var pid in ids)
        //     await _reco.TrackAsync("purchase", pid, userId, sid);
    }
    catch
    {
        // No rompas la compra si fallan los eventos. Opcionalmente registra log.
    }

    // 8) Respuesta
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
