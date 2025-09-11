using System.ComponentModel.DataAnnotations.Schema;

namespace ImportadoraSonib.Domain.Entities;

public class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}
