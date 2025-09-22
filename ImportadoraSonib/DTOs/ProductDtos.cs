using System.ComponentModel.DataAnnotations;

namespace ImportadoraSonib.DTOs
{
    // ==== Públicos (usados por ProductsController en endpoints GET públicos) ====
    public record ProductListItemDto(
        int Id, string Name, string Slug, decimal Price, string ImageUrl, int Stock, string Category
    );

    public record ProductDetailDto(
        int Id, string Name, string Slug, string Description, decimal Price,
        string ImageUrl, int Stock, int CategoryId, string Category
    );

    // ==== Admin (usados por endpoints /api/products/admin*) ====
    public record ProductAdminListDto(
        int Id, string Name, string Slug, decimal Price, int Stock,
        bool IsActive, bool IsDeleted, int CategoryId, string Category, string RowVersion // Base64
    );

    public record ProductAdminDetailDto(
        int Id, string Name, string Slug, string Description, string Tags, decimal Price,
        string ImageUrl, int Stock, bool IsActive, int CategoryId, string Category, string RowVersion // Base64
    );

    public class ProductCreateDto
    {
        [Required, MaxLength(150)] public string Name { get; set; } = default!;
        [Required, MaxLength(150)] public string Slug { get; set; } = default!;
        [MaxLength(500)] public string Description { get; set; } = "";
        [MaxLength(200)] public string Tags { get; set; } = "";
        [Range(0, 999999999)] public decimal Price { get; set; }
        [MaxLength(300)] public string ImageUrl { get; set; } = "";
        [Range(0, int.MaxValue)] public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
        [Required] public int CategoryId { get; set; }
    }

    // Para UPDATE, el cliente debe enviar la RowVersion (Base64)
    public class ProductUpdateDto : ProductCreateDto
    {
        [Required] public string RowVersion { get; set; } = default!;
    }
}
