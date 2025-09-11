using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImportadoraSonib.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(150)]
    public string Slug { get; set; } = default!;

    [MaxLength(500)]
    public string Description { get; set; } = "";

    [MaxLength(200)]
    public string Tags { get; set; } = "";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(300)]
    public string ImageUrl { get; set; } = "";

    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Control de concurrencia
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // Auditoría
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
