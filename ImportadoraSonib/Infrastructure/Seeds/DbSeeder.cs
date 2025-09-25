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

        // Asegura roles base
        foreach (var r in new[] { "Admin", "Customer" })
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new IdentityRole(r));

        // 2) Admin “único” desde configuración
        var adminEmail = cfg["Admin:Email"] ?? "admin@sonib.com";
        var adminPass  = cfg["Admin:Password"] ?? "Admin#12345";

        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var created = await userMgr.CreateAsync(admin, adminPass);
            if (created.Succeeded)
                await userMgr.AddToRoleAsync(admin, "Admin");
            else
                throw new Exception("No se pudo crear el admin inicial: " +
                                    string.Join("; ", created.Errors.Select(e => e.Description)));
        }
        else
        {
            // reset solo si lo pides por config
            var forceReset = bool.TryParse(cfg["Admin:ForceResetOnStartup"], out var b) && b;
            if (forceReset)
            {
                var token = await userMgr.GeneratePasswordResetTokenAsync(admin);
                var reset = await userMgr.ResetPasswordAsync(admin, token, adminPass);
                if (!reset.Succeeded)
                    throw new Exception("No se pudo resetear la contraseña del admin: " +
                                        string.Join("; ", reset.Errors.Select(e => e.Description)));
            }

            if (!await userMgr.IsInRoleAsync(admin, "Admin"))
                await userMgr.AddToRoleAsync(admin, "Admin");

            if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true;
                await userMgr.UpdateAsync(admin);
            }
        }

        // 3)  NINGÚN otro usuario tenga Admin
        var allAdmins = await userMgr.GetUsersInRoleAsync("Admin");
        foreach (var u in allAdmins)
        {
            if (!string.Equals(u.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                await userMgr.RemoveFromRoleAsync(u, "Admin");
                //  si no tiene rol, ponlo como Customer
                var roles = await userMgr.GetRolesAsync(u);
                if (roles.Count == 0)
                    await userMgr.AddToRoleAsync(u, "Customer");
            }
        }

        await db.SaveChangesAsync();
    }
}
