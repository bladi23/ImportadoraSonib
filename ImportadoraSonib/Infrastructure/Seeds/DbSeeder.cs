using ImportadoraSonib.Domain.Entities;
using ImportadoraSonib.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Infrastructure.Seeds;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IServiceProvider? sp = null)
    {
        if (!await db.Categories.AnyAsync())
        {
            var cats = new[] {
                new Category{ Name="Tecnología", Slug="tecnologia"},
                new Category{ Name="Electrodomésticos", Slug="electrodomesticos"},
                new Category{ Name="Motos eléctricas", Slug="motos-electricas"}
            };
            db.Categories.AddRange(cats);
            await db.SaveChangesAsync();

            db.Products.AddRange(
                new Product { CategoryId = cats[0].Id, Name = "Smartphone X", Slug = "smartphone-x", Price = 299, Stock = 10, ImageUrl = "/uploads/smartphone.jpg", Tags = "android,5g,128gb" },
                new Product { CategoryId = cats[1].Id, Name = "Licuadora Pro", Slug = "licuadora-pro", Price = 49, Stock = 25, ImageUrl = "/uploads/licuadora.jpg", Tags = "cocina,potente" },
                new Product { CategoryId = cats[2].Id, Name = "Moto E-2000", Slug = "moto-e-2000", Price = 1200, Stock = 3, ImageUrl = "/uploads/moto.jpg", Tags = "moto,eco,electrica" }
            );
            await db.SaveChangesAsync();
        }

        // Admin por defecto 
        if (sp != null)
        {
            var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = sp.GetRequiredService<UserManager<IdentityUser>>();
            if (!await roleMgr.RoleExistsAsync("Admin"))
                await roleMgr.CreateAsync(new IdentityRole("Admin"));

            var admin = await userMgr.FindByEmailAsync("admin@sonib.com");
            if (admin == null)
            {
                admin = new IdentityUser { UserName = "admin@sonib.com", Email = "admin@sonib.com", EmailConfirmed = true };
                await userMgr.CreateAsync(admin, "Admin#12345");
                await userMgr.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
