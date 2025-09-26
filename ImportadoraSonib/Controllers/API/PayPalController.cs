using ImportadoraSonib.Data;
using ImportadoraSonib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class PayPalController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PayPalService _pp;
    private readonly IConfiguration _cfg;

    public PayPalController(ApplicationDbContext db, PayPalService pp, IConfiguration cfg)
    { _db = db; _pp = pp; _cfg = cfg; }

    public record CreateReq(int OrderId);

    // 1) Crear orden PayPal y devolver approveUrl para redirigir
    [Authorize]
    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateReq req)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == req.OrderId);
        if (order is null) return NotFound("Orden no existe");
        if (order.Status == "Pagado") return BadRequest("La orden ya está pagada");
        if (order.Items.Count == 0) return BadRequest("Orden sin items");

        var publicRoot = _cfg["PublicRootUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var clientUrl  = _cfg["Payments:ClientUrl"] ?? "http://localhost:4200";

        var returnUrl = $"{publicRoot}/api/paypal/return?orderId={order.Id}";
        var cancelUrl = $"{clientUrl}/checkout/cancel?orderId={order.Id}";

        var (ppId, approveUrl) = await _pp.CreateOrderAsync(order.Total, "USD", returnUrl, cancelUrl);

        order.PaymentProvider = "paypal";
        order.CheckoutSessionId = ppId;      // guarda id de PayPal
        order.PaymentExternalId = ppId;
        await _db.SaveChangesAsync();

        return Ok(new { approveUrl });
    }

    // 2) PayPal redirige aquí con ?token=PAYPAL_ORDER_ID
    [HttpGet("return")]
    public async Task<IActionResult> Return([FromQuery] int orderId, [FromQuery] string token)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return NotFound();

        try
        {
            await _pp.CaptureOrderAsync(token);
            order.Status = "Pagado";
            order.PaidAt = DateTime.UtcNow;

            // limpia carrito del usuario si existe
            if (!string.IsNullOrEmpty(order.UserId))
            {
                var cart = _db.CartItems.Where(c => c.UserId == order.UserId);
                _db.CartItems.RemoveRange(cart);
            }

            await _db.SaveChangesAsync();

            var clientUrl = _cfg["Payments:ClientUrl"] ?? "http://localhost:4200";
            return Redirect($"{clientUrl}/checkout/success?orderId={order.Id}");
        }
        catch
        {
            var clientUrl = _cfg["Payments:ClientUrl"] ?? "http://localhost:4200";
            return Redirect($"{clientUrl}/checkout/cancel?orderId={order.Id}");
        }
    }
}
