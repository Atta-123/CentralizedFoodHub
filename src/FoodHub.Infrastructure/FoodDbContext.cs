using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure;
public class FoodDbContext(DbContextOptions<FoodDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderHistory> OrderHistories => Set<OrderHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });
        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Cuisine).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(300).IsRequired();
            entity.Property(x => x.CommissionPercent).HasPrecision(5, 2);
            entity.HasIndex(x => x.OwnerId).IsUnique();
            entity.HasIndex(x => new { x.Latitude, x.Longitude });
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("Restaurants", table =>
            {
                table.HasCheckConstraint("CK_Restaurants_Commission", "[CommissionPercent] BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_Restaurants_Location", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
                table.HasCheckConstraint("CK_Restaurants_Radius", "[DeliveryRadiusKm] BETWEEN 0.1 AND 50");
            });
        });
        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.RestaurantId, x.IsAvailable });
            entity.HasOne(x => x.Restaurant).WithMany(x => x.Menu).HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("MenuItems", table => table.HasCheckConstraint("CK_MenuItems_Price", "[PriceCents] > 0"));
        });
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RestaurantName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CommissionPercent).HasPrecision(5, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.CustomerId, x.CreatedAt });
            entity.HasIndex(x => new { x.RestaurantId, x.Status, x.CreatedAt });
            entity.HasIndex(x => new { x.CustomerId, x.IdempotencyKey }).IsUnique();
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("Orders", table =>
            {
                table.HasCheckConstraint("CK_Orders_Commission", "[CommissionPercent] BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_Orders_Totals", "[SubtotalCents] >= 0 AND [CommissionCents] >= 0 AND [RestaurantPayableCents] >= 0 AND [SubtotalCents] = [CommissionCents] + [RestaurantPayableCents]");
            });
        });
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            // MenuItemId is a historical reference; name/price are checkout snapshots.
            entity.HasOne<MenuItem>().WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("OrderItems", table => table.HasCheckConstraint("CK_OrderItems_QuantityPrice", "[Quantity] BETWEEN 1 AND 20 AND [UnitPriceCents] > 0"));
        });
        modelBuilder.Entity<OrderHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.OrderId, x.ChangedAt });
            entity.HasOne(x => x.Order).WithMany(x => x.History).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
