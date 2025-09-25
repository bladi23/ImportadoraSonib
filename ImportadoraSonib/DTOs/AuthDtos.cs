using System.ComponentModel.DataAnnotations;

namespace ImportadoraSonib.DTOs;

public record RegisterDto([Required, EmailAddress] string Email,
                          [Required, MinLength(6)] string Password);

public record LoginDto([Required, EmailAddress] string Email,
                       [Required] string Password);

public record LoginRes(string Token, string Email, IList<string> Roles);

public record ChangePwdDto([Required] string CurrentPassword,
                           [Required, MinLength(8)] string NewPassword);

public record ChangeEmailDto([Required, EmailAddress] string NewEmail);

public record ForgotPwdDto([Required, EmailAddress] string Email);

public record ResetPwdDto([Required, EmailAddress] string Email,
                          [Required] string Token,
                          [Required, MinLength(8)] string NewPassword);
