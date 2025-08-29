using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImportadoraSonib.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public string Description { get; set; } = "";
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = "";
        public string Tags { get; set; } = "Celular ";   // "android,5g,128gb"
        public int Stock { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        [Timestamp] public byte[] RowVersion { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
