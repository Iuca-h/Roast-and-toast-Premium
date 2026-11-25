using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Webshop.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentVerificationAttempt> PaymentVerificationAttempts => Set<PaymentVerificationAttempt>();
    public DbSet<UserPaymentVerificationAttempt> UserPaymentVerificationAttempts => Set<UserPaymentVerificationAttempt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // PRODUCTS
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.Model).HasMaxLength(100);
            entity.Property(p => p.PriceCents).HasColumnType("int unsigned");
            entity.Property(p => p.PowerKw).HasPrecision(6, 2);
            entity.Property(p => p.Description).HasColumnType("text");
            entity.Property(p => p.ImageData).HasColumnType("longblob");
            entity.Property(p => p.ImageContentType).HasMaxLength(100);
            entity.Property(p => p.ImageFileName).HasMaxLength(255);
        });

        // STOCK: exactly one row per product
        builder.Entity<Stock>(entity =>
        {
            entity.ToTable("Stocks");
            entity.HasKey(s => s.Id);

            entity.HasIndex(s => s.ProductId).IsUnique();

            entity.HasOne(s => s.Product)
                .WithOne(p => p.Stock)
                .HasForeignKey<Stock>(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CARTS
        builder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(c => c.Id);

            entity.HasIndex(c => c.UserId).IsUnique();

            entity.HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CART ITEMS
        builder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(ci => ci.Id);

            entity.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();

            entity.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ORDERS
        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);

            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.CreatedAt);

            entity.Property(o => o.TotalPriceCents).HasColumnType("bigint");
            entity.Property(o => o.Currency).HasColumnType("char(3)");

            entity.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ORDER ITEMS
        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(oi => oi.Id);

            entity.HasIndex(oi => oi.OrderId);
            entity.HasIndex(oi => oi.ProductId);

            entity.Property(oi => oi.UnitPriceCents).HasColumnType("bigint");
            entity.Property(oi => oi.SubtotalCents).HasColumnType("bigint");

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PAYMENTS
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(p => p.Id);

            entity.HasIndex(p => p.TransactionId).IsUnique();
            entity.HasIndex(p => p.OrderId);

            entity.Property(p => p.AmountCents).HasColumnType("bigint");
            entity.Property(p => p.Currency).HasColumnType("char(3)");
            entity.Property(p => p.CardLast4).HasColumnType("char(4)");

            entity.HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PAYMENT VERIFICATION ATTEMPTS
        builder.Entity<PaymentVerificationAttempt>(entity =>
        {
            entity.ToTable("PaymentVerificationAttempts");
            entity.HasKey(a => a.Id);

            entity.HasIndex(a => a.OrderId).IsUnique();
            entity.Property(a => a.FailedAttempts).HasColumnType("int");

            entity.HasOne(a => a.Order)
                .WithMany()
                .HasForeignKey(a => a.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // USER PAYMENT VERIFICATION ATTEMPTS
        builder.Entity<UserPaymentVerificationAttempt>(entity =>
        {
            entity.ToTable("UserPaymentVerificationAttempts");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.UserId).HasMaxLength(255);
            entity.HasIndex(a => a.UserId).IsUnique();
            entity.Property(a => a.FailedAttempts).HasColumnType("int");

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}