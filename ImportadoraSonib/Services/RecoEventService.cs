using ImportadoraSonib.Data;
using ImportadoraSonib.Domain.Entities;

namespace ImportadoraSonib.Services;

public class RecoEventService
{
    private readonly ApplicationDbContext _db;
    public RecoEventService(ApplicationDbContext db) { _db = db; }

    // ✔️ Sobrecarga corta (por si la llamas con 2 args)
    public Task TrackAsync(string type, int productId)
        => TrackAsync(type, productId, null, null);

    // ✔️ Sobrecarga completa (la que usa OrdersController)
    public async Task TrackAsync(string type, int productId, string? userId, string? sessionId)
    {
        var ev = new ProductEvent
        {
            EventType = type,
            ProductId = productId,
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProductEvents.Add(ev);
        await _db.SaveChangesAsync();
    }
}
