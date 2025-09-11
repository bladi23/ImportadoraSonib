using System.ComponentModel.DataAnnotations;

namespace ImportadoraSonib.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }

    
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public string? UserId { get; set; }
    public string? SessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
