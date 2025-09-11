using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImportadoraSonib.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string? UserId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "Pendiente"; // Pendiente | Confirmado | Cancelado

    public ICollection<OrderDetail> Items { get; set; } = new List<OrderDetail>();
}

