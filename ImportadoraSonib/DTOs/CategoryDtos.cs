using System.ComponentModel.DataAnnotations;

namespace ImportadoraSonib.DTOs;

public record CategoryDto(int Id, string Name, string Slug);

public record CategoryCreateDto([Required, MaxLength(100)] string Name,
                                [Required, MaxLength(100)] string Slug,
                                bool IsActive = true);
