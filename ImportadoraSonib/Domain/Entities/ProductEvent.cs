namespace ImportadoraSonib.Domain.Entities;

public class ProductEvent
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string EventType { get; set; } = default!; // view, add_to_cart, purchase, etc.
    public string? UserId { get; set; }
    public string SessionId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
