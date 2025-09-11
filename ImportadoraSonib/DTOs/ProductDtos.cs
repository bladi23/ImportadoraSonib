namespace ImportadoraSonib.DTOs;

public record ProductListDto(int Id, string Name, string Slug, decimal Price, string ImageUrl, int Stock, string Category);
public record ProductDetailDto(int Id, string Name, string Slug, string Description, decimal Price, string ImageUrl, int Stock, string Category);
public record ProductCreateDto(string Name, string Slug, string Description, decimal Price, string ImageUrl, int Stock, int CategoryId, bool IsActive);
public record ProductUpdateDto(int Id, string Name, string Slug, string Description, decimal Price, string ImageUrl, int Stock, int CategoryId, bool IsActive, byte[] RowVersion);
