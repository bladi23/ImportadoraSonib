using ImportadoraSonib.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImportadoraSonib.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Product>().Property(p => p.RowVersion).IsRowVersion();

        // Precio 
        builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);

        // Filtro global 
        builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted && p.IsActive);

        // CartItem 
        builder.Entity<CartItem>().HasIndex(c => new { c.UserId, c.ProductId });
        builder.Entity<CartItem>().HasIndex(c => new { c.SessionId, c.ProductId });
       
        builder.Entity<CartItem>()
    .HasOne(ci => ci.Product)
    .WithMany()
    .HasForeignKey(ci => ci.ProductId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<OrderDetail>()
    .HasOne(od => od.Product)
    .WithMany()
    .HasForeignKey(od => od.ProductId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.SetNull);

    }
}
