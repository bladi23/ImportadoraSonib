using ImportadoraSonib.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {

        base.OnModelCreating(builder);

        builder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Product>().Property(p => p.RowVersion).IsRowVersion();
        builder.Entity<Product>()
        .Property(p => p.Price)
        .HasPrecision(18, 2); // 18 dígitos en total, 2 decimales


        builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted && p.IsActive);
    }
}
