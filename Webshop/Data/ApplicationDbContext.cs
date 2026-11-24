using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Webshop.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.PriceCents)
                .HasColumnType("int unsigned");

            entity.Property(p => p.PowerKw)
                .HasPrecision(6, 2);

            entity.Property(p => p.Description)
                .HasColumnType("text");

            entity.Property(p => p.ImageData)
                .HasColumnType("longblob");
        });

        builder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");

            entity.HasKey(c => c.Id);

            entity.HasIndex(c => c.UserId)
                .IsUnique();

            entity.HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");

            entity.HasKey(ci => ci.Id);

            entity.HasIndex(ci => new { ci.CartId, ci.ProductId })
                .IsUnique();

            entity.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}