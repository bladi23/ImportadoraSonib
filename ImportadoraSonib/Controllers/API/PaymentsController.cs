using System.Security.Claims;
using ImportadoraSonib.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _cfg;

    public PaymentsController(ApplicationDbContext db, IConfiguration cfg)
    {
        _db = db; _cfg = cfg;
    }

    // -------- DEMO PROVIDER --------

    public record DemoCreateReq(int OrderId);

    /// Crea una "sesión" DEMO y devuelve una URL (del front) para simular pago
    [Authorize]
    [HttpPost("demo/create-session")]
    public async Task<IActionResult> DemoCreateSession([FromBody] DemoCreateReq req)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId);

        if (order is null) return NotFound("Orden no existe.");
        if (order.Status == "Pagado") return BadRequest("La orden ya está pagada.");
        if (order.Items.Count == 0) return BadRequest("Orden sin items.");

        var sessionId = "DEMO_" + Guid.NewGuid().ToString("N");
        var clientUrl = _cfg["Payments:ClientUrl"] ?? "http://localhost:4200";

        // (Opcional) URLs para tu front
        var successUrl = $"{clientUrl}/checkout/success?orderId={order.Id}&session_id={sessionId}";
        var cancelUrl  = $"{clientUrl}/checkout/cancel?orderId={order.Id}";

        order.PaymentProvider = "demo";
        order.CheckoutSessionId = sessionId;
        order.PaymentExternalId = sessionId;
        await _db.SaveChangesAsync();

        // Ruta a tu página de “pago” demo (si implementas el front)
        var checkoutUrl = $"{clientUrl}/demo-checkout?orderId={order.Id}&sessionId={sessionId}&amount={order.Total:0.00}";

        return Ok(new { checkoutUrl, sessionId, successUrl, cancelUrl });
    }

    public record DemoConfirmReq(int OrderId, string SessionId, string Outcome);
    // Outcome: "approved" | "declined" | "canceled"

    /// Confirma la sesión DEMO (cambia estado de la orden según Outcome)
    /// 
    /// 
   // using System.Security.Claims;  // ya lo tienes arriba

[Authorize]
[HttpPost("demo/confirm")]
public async Task<IActionResult> DemoConfirm([FromBody] DemoConfirmReq req)
{
    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId);
    if (order is null) return NotFound("Orden no existe.");

    if (!string.Equals(order.CheckoutSessionId, req.SessionId, StringComparison.OrdinalIgnoreCase))
        return BadRequest("SessionId inválido para esta orden.");

    switch (req.Outcome?.ToLowerInvariant())
    {
       case "approved":
{
    // Carga la orden con items y productos para validar stock al momento de pagar
    var full = await _db.Orders
        .Include(x => x.Items)
        .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(x => x.Id == order.Id);

    if (full is null) return NotFound("Orden no existe.");

    // Validar stock disponible
    foreach (var d in full.Items)
    {
        if (d.Product is null || d.Product.IsDeleted || !d.Product.IsActive)
            return Conflict(new { message = $"El producto {d.ProductId} ya no está disponible." });

        if (d.Product.Stock < d.Quantity)
            return Conflict(new { message = $"Stock insuficiente para {d.Product.Name}. Disponible: {d.Product.Stock}, solicitado: {d.Quantity}." });
    }

    // Descontar stock (control de concurrencia optimista por RowVersion si la tienes en Product)
    try
    {
        foreach (var d in full.Items)
        {
            d.Product!.Stock -= d.Quantity;
            d.Product.UpdatedAt = DateTime.UtcNow;
        }

        order.Status = "Pagado";
        order.PaidAt = DateTime.UtcNow;

        // Limpia carrito: por UserId y también por SessionId (si existiese en tu CartService)
        if (!string.IsNullOrEmpty(order.UserId))
        {
            var userCart = await _db.CartItems.Where(c => c.UserId == order.UserId).ToListAsync();
            _db.CartItems.RemoveRange(userCart);
        }

        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict(new { message = "Conflicto de concurrencia al descontar stock. Por favor reintenta." });
    }

    return Ok(new { ok = true, status = order.Status });
}

        case "declined":
            order.Status = "Pendiente";
            await _db.SaveChangesAsync();
            return Ok(new { ok = false, status = order.Status, reason = "Tarjeta rechazada (DEMO)" });

        case "canceled":
            order.Status = "Cancelado";
            await _db.SaveChangesAsync();
            return Ok(new { ok = false, status = order.Status });

        default:
            return BadRequest("Outcome inválido (usa approved | declined | canceled).");
    }
}

    /// Ver estado y detalles de una orden
    [Authorize]
    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetOrder(int orderId)
    {
        var o = await _db.Orders
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (o is null) return NotFound();
        return Ok(new
        {
            o.Id, o.Status, o.Total, o.OrderDate, o.PaidAt,
            o.PaymentProvider, o.PaymentExternalId, o.CheckoutSessionId,
            Items = o.Items.Select(i => new { i.ProductId, i.Quantity, i.UnitPrice, Product = i.Product?.Name })
        });
    }
}
