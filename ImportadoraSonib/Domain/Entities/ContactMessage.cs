using System.ComponentModel.DataAnnotations;

namespace ImportadoraSonib.Domain.Entities;

public class ContactMessage
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = default!;

    [Required, MaxLength(1500)]
    public string Message { get; set; } = default!;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
