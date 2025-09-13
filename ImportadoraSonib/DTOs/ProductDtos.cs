using System.ComponentModel.DataAnnotations;

namespace ImportadoraSonib.DTOs;

public record ProductListItemDto(int Id, string Name, string Slug, decimal Price, string ImageUrl, int Stock, string Category);

public record ProductDetailDto(int Id, string Name, string Slug, string Description, decimal Price, string ImageUrl, int Stock, int CategoryId, string Category);

public record ProductCreateDto(
    [Required, MaxLength(150)] string Name,
    [Required, MaxLength(150)] string Slug,
    [MaxLength(500)] string? Description,
    [Range(0, 9999999)] decimal Price,
    [MaxLength(300)] string? ImageUrl,
    [Range(0, int.MaxValue)] int Stock,
    [Required] int CategoryId,
    bool IsActive = true);

public record ProductUpdateDto(
    [Required] int Id,
    [Required, MaxLength(150)] string Name,
    [Required, MaxLength(150)] string Slug,
    [MaxLength(500)] string? Description,
    [Range(0, 9999999)] decimal Price,
    [MaxLength(300)] string? ImageUrl,
    [Range(0, int.MaxValue)] int Stock,
    [Required] int CategoryId,
    bool IsActive,
    byte[] RowVersion);
