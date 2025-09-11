using ImportadoraSonib.Data;
using ImportadoraSonib.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        var items = await GetCurrentAsync();
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
    }
}
