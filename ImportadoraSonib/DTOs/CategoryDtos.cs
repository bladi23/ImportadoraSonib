namespace ImportadoraSonib.DTOs;

public record CategoryDto(int Id, string Name, string Slug);
public record CategoryCreateDto(string Name, string Slug, bool IsActive);
