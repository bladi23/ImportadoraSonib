using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ImportadoraSonib.DTOs;
using Microsoft.AspNetCore.Authorization;
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
    var exists = await _users.FindByEmailAsync(dto.Email);
    if (exists != null)
        return Conflict("El correo ya está registrado. Inicia sesión o usa 'Olvidé mi contraseña'.");

    var user = new IdentityUser { UserName = dto.Email, Email = dto.Email, EmailConfirmed = true };
    var res = await _users.CreateAsync(user, dto.Password);
    if (!res.Succeeded) return BadRequest(res.Errors.Select(e => e.Description));

    var roleRes = await _users.AddToRoleAsync(user, "Customer");
    if (!roleRes.Succeeded) return StatusCode(500, roleRes.Errors.Select(e => e.Description));

    return Ok();
}



   [HttpPost("login")]
[AllowAnonymous]
public async Task<ActionResult<LoginRes>> Login([FromBody] LoginDto dto)
{
    var user = await _users.FindByEmailAsync(dto.Email);
    if (user is null) return Unauthorized();

    var ok = await _signIn.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
    if (!ok.Succeeded) return Unauthorized();

    // 👇 trae los roles reales del usuario
    var roles = await _users.GetRolesAsync(user);

    // 👇 genera el JWT incluyendo claims de rol
    var token = await BuildTokenAsync(user, roles);

    return Ok(new LoginRes(token, user.Email!, roles));
}


    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
    // 1) Saca datos de los claims del token
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("nameid"); // por si algún handler mapea distinto
    var email  = User.FindFirstValue(ClaimTypes.Email)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

    // 2) Recupera (si puedes) el usuario desde la BD
    IdentityUser? u = null;
    if (!string.IsNullOrEmpty(userId))
        u = await _users.FindByIdAsync(userId);
    if (u == null && !string.IsNullOrEmpty(email))
        u = await _users.FindByEmailAsync(email!);

    // 3) Roles: si el user existe, usa BD; si no, usa lo que venga en el token
    string[] roles;
    if (u != null)
    {
        var fromDb = await _users.GetRolesAsync(u);
        roles = fromDb.ToArray();
    }
    else
    {
        roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).Distinct().ToArray();
    }

    return Ok(new
    {
        email = u?.Email ?? email,
        userId = u?.Id ?? userId,
        roles
    });
}


    // ---- Cambiar contraseña (autenticado) ----
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePwdDto dto)
    {
        var user = await _users.GetUserAsync(User);
        var res  = await _users.ChangePasswordAsync(user!, dto.CurrentPassword, dto.NewPassword);
        return res.Succeeded ? NoContent() : BadRequest(res.Errors);
    }

    // ---- Cambiar email (autenticado) ----
    [HttpPost("change-email")]
    [Authorize]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
    {
        var user = await _users.GetUserAsync(User);
        var token = await _users.GenerateChangeEmailTokenAsync(user!, dto.NewEmail);
        var res   = await _users.ChangeEmailAsync(user!, dto.NewEmail, token);
        if (!res.Succeeded) return BadRequest(res.Errors);
        user!.UserName = dto.NewEmail; // login por email
        await _users.UpdateAsync(user);
        return NoContent();
    }

    // ---- Olvidé mi contraseña (dev: devuelve token; prod: enviar por email) ----
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPwdDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null) return Ok(new { ok = true }); // no revelar existencia

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        // DEV: Devolver el token para probar en Swagger (NO en producción)
        return Ok(new { ok = true, resetToken = token });
    }

    // ---- Reset password con token ----
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPwdDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null) return BadRequest("Usuario no encontrado.");
        var res  = await _users.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        return res.Succeeded ? NoContent() : BadRequest(res.Errors);
    }

    private async Task<string> BuildTokenAsync(IdentityUser user, IList<string> roles)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
        new Claim(ClaimTypes.Email, user.Email!),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    // 👇 agrega cada rol al token (claim "role")
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

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
