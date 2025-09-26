using ImportadoraSonib.Data;
using ImportadoraSonib.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ImportadoraSonib.Services;

public class CartService
{
    private readonly ApplicationDbContext _db;

    private readonly IHttpContextAccessor _http;

    public CartService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db; _http = http;
    }

    private string GetSessionId()
    {
        var ctx = _http.HttpContext!;
        var sid = ctx.Session.GetString("cartSession");
        if (string.IsNullOrEmpty(sid))
        {
            sid = Guid.NewGuid().ToString("N");
            ctx.Session.SetString("cartSession", sid);
        }
        return sid;
    }

    private string? GetUserId() => _http.HttpContext!.User?.Identity?.IsAuthenticated == true
        ? _http.HttpContext!.User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value
        : null;

    public async Task AddAsync(int productId, int qty = 1)
    {
        var userId = GetUserId();
        var sessionId = userId is null ? GetSessionId() : null;

        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId && c.SessionId == sessionId);

        if (item is null)
        {
            item = new CartItem { ProductId = productId, Quantity = qty, UserId = userId, SessionId = sessionId };
            _db.CartItems.Add(item);
        }
        else
        {
            item.Quantity += qty;
            item.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(int productId)
    {
        var userId = GetUserId();
        var sessionId = userId is null ? GetSessionId() : null;

        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId && c.SessionId == sessionId);

        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<CartItem>> GetCurrentAsync()
    {
        var userId = GetUserId();
        var sessionId = userId is null ? GetSessionId() : null;

        return await _db.CartItems
            .Where(c => c.UserId == userId && userId != null || c.SessionId == sessionId && sessionId != null)
            .Include(c => c.Product)
            .ToListAsync();
    }

    public async Task ClearAsync()
{
    var http = _http.HttpContext;
    var userId = http?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var sessionId = http?.Session?.Id;

    var q = _db.CartItems.AsQueryable();

    if (!string.IsNullOrEmpty(userId))
        q = q.Where(c => c.UserId == userId);
    else if (!string.IsNullOrEmpty(sessionId))
        q = q.Where(c => c.SessionId == sessionId);
    else
        return;

    var all = await q.ToListAsync();
    _db.CartItems.RemoveRange(all);
    await _db.SaveChangesAsync();
}
}
