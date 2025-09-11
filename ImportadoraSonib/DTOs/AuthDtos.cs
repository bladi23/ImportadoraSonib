namespace ImportadoraSonib.DTOs;

public record RegisterDto(string Email, string Password);
public record LoginDto(string Email, string Password);
public record LoginRes(string Token, string Email);
