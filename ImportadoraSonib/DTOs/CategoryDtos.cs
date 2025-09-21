namespace ImportadoraSonib.DTOs
{
    public record CategoryDto(int Id, string Name, string Slug, bool IsActive = true);
    public record CategoryCreateDto(string Name, string Slug, bool IsActive = true);
    public record CategoryUpdateDto(string Name, string Slug, bool IsActive = true);
}
