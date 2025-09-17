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
    public string? PaymentProvider { get; set; }     // demo | payphone | kushki | ...
    public string? PaymentExternalId { get; set; }   // id que devuelve el proveedor o DEMO
    public string? CheckoutSessionId { get; set; }   // id de la sesión de pago
    public DateTime? PaidAt { get; set; }            // fecha de pago

    public ICollection<OrderDetail> Items { get; set; } = new List<OrderDetail>();
}

