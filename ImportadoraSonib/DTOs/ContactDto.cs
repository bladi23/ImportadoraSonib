using System.ComponentModel.DataAnnotations;

namespace ImportadoraSonib.DTOs;

public record ContactDto([Required, MaxLength(120)] string Name,
                         [Required, EmailAddress, MaxLength(200)] string Email,
                         [Required, MaxLength(1500)] string Message);
