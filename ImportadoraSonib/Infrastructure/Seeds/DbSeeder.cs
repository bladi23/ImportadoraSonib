using ImportadoraSonib.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ImportadoraSonib.Infrastructure.Seeds;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, IConfiguration cfg)
    {
        await db.Database.EnsureCreatedAsync();

        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = sp.GetRequiredService<UserManager<IdentityUser>>();

        var roles = new[] { "Admin", "Customer" }; // solo estos dos
        foreach (var r in roles)
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new IdentityRole(r));

        var adminEmail = cfg["Admin:Email"] ?? "admin@sonib.com";
var adminPass  = cfg["Admin:Password"] ?? "Admin#12345";

var admin = await userMgr.FindByEmailAsync(adminEmail);
if (admin == null)
{
    admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
    var created = await userMgr.CreateAsync(admin, adminPass);
    if (created.Succeeded) await userMgr.AddToRoleAsync(admin, "Admin");
}
else
{
    var token = await userMgr.GeneratePasswordResetTokenAsync(admin);
    await userMgr.ResetPasswordAsync(admin, token, adminPass);
    if (!await userMgr.IsInRoleAsync(admin, "Admin"))
        await userMgr.AddToRoleAsync(admin, "Admin");
    if (!admin.EmailConfirmed)
    {
        admin.EmailConfirmed = true;
        await userMgr.UpdateAsync(admin);
    }
}
        //  datos mínimos de demo si la tabla está vacía
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.Add(new Domain.Entities.Category { Name = "Tecnología", Slug = "tecnologia" });
            db.Categories.Add(new Domain.Entities.Category { Name = "Hogar", Slug = "hogar" });
        }
        await db.SaveChangesAsync();
    }
}
