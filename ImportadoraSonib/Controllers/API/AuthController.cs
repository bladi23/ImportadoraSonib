using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ImportadoraSonib.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ImportadoraSonib.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _users;
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly IConfiguration _cfg;

    public AuthController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, IConfiguration cfg)
    {
        _users = users; _signIn = signIn; _cfg = cfg;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email, EmailConfirmed = true };
        var res = await _users.CreateAsync(user, dto.Password);
        if (!res.Succeeded) return BadRequest(res.Errors);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginRes>> Login(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized();

        var ok = await _signIn.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!ok.Succeeded) return Unauthorized();

        var token = BuildToken(user);
        return Ok(new LoginRes(token, user.Email!));
    }

    private string BuildToken(IdentityUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim> {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
