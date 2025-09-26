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
    public DbSet<ProductEvent> ProductEvents => Set<ProductEvent>(); // 👈 solo UNA

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Product>().Property(p => p.RowVersion).IsRowVersion();
        builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted && p.IsActive);

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

        // ✔️ Configuración de ProductEvent
        builder.Entity<ProductEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
            e.Property(x => x.SessionId).HasMaxLength(100).IsRequired();

            e.HasIndex(x => new { x.ProductId, x.EventType });
            e.HasIndex(x => new { x.UserId, x.EventType });

            e.HasOne<Product>()
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
