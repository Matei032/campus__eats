using CampusEats.Backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== USER CONFIGURATION =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(u => u.Email)
                .IsUnique();
            
            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(u => u.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(u => u.StudentId)
                .HasMaxLength(50);
            
            entity.Property(u => u.LoyaltyPoints)
                .HasDefaultValue(0);
            
            // Default value for CreatedAt
            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("NOW()");
        });

        // ===== PRODUCT CONFIGURATION =====
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
    
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
    
            entity.Property(p => p.Description)
                .HasMaxLength(500);
    
            entity.Property(p => p.Price)
                .HasColumnType("decimal(10,2)")
                .IsRequired();
    
            entity.Property(p => p.Category)
                .IsRequired()
                .HasMaxLength(50);
    
            entity.Property(p => p.ImageUrl)
                .HasMaxLength(500);
    
            entity.Property(p => p.DietaryRestrictions)
                .HasMaxLength(100);
    
            entity.Property(p => p.IsAvailable)
                .HasDefaultValue(true);
            
            entity.Property(p => p.Allergens)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb");
    
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("NOW()");
    
            entity.HasIndex(p => p.Category);
            entity.HasIndex(p => p.IsAvailable);
        });

        // ===== ORDER CONFIGURATION =====
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(o => o.OrderNumber)
                .IsUnique();

            entity.Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(o => o.PaymentStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(o => o.PaymentMethod)
                .HasMaxLength(50);

            entity.Property(o => o.Notes)
                .HasMaxLength(500);

            entity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== ORDER ITEM CONFIGURATION =====
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);

            entity.Property(oi => oi.Quantity)
                .IsRequired();

            entity.Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(oi => oi.Subtotal)
                .HasColumnType("decimal(18,2)");

            entity.Property(oi => oi.SpecialInstructions)
                .HasMaxLength(500);
            
            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== PAYMENT CONFIGURATION =====
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Amount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            
            entity.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(p => p.Method)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(p => p.StripePaymentIntentId)
                .HasMaxLength(100);
            
            entity.Property(p => p.StripeSessionId)
                .HasMaxLength(100);
            
            entity.Property(p => p.FailureReason)
                .HasMaxLength(500);
            
            // Default value for CreatedAt
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("NOW()");
            
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.StripePaymentIntentId);
        });

        // ===== LOYALTY TRANSACTION CONFIGURATION =====
        modelBuilder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.HasKey(lt => lt.Id);
            
            entity.Property(lt => lt.PointsChange)
                .IsRequired();
            
            entity.Property(lt => lt.Type)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(lt => lt.Description)
                .IsRequired()
                .HasMaxLength(200);
            
            // Default value for CreatedAt
            entity.Property(lt => lt.CreatedAt)
                .HasDefaultValueSql("NOW()");
            
            entity.HasOne(lt => lt.User)
                .WithMany(u => u.LoyaltyTransactions)
                .HasForeignKey(lt => lt.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            //entity.HasOne(lt => lt.Order)
                //.WithMany(o => o.LoyaltyTransactions)
                //.HasForeignKey(lt => lt.OrderId)
                //.OnDelete(DeleteBehavior.SetNull)
                //.IsRequired(false);
            
            entity.HasIndex(lt => lt.UserId);
            entity.HasIndex(lt => lt.CreatedAt);
        });
    }
}