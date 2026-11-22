using ArtisanHub.Models;
using Microsoft.EntityFrameworkCore;

namespace ArtisanHub.Data;

/// <summary>
/// EF Core context mapped to the tables and views created by Database/ArtisanHub_Database.sql.
/// The database is built by that script (not by EF migrations), so the mapping below matches it exactly.
/// </summary>
public class ArtisanHubContext : DbContext
{
    public ArtisanHubContext(DbContextOptions<ArtisanHubContext> options) : base(options)
    {
    }

    // Accounts module
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Artist> Artists => Set<Artist>();

    // Catalog module
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CustomRequest> CustomRequests => Set<CustomRequest>();

    // Orders module
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Review> Reviews => Set<Review>();

    // SQL views (read-only)
    public DbSet<OrderTotal> OrderTotals => Set<OrderTotal>();
    public DbSet<SalesByArtist> SalesByArtist => Set<SalesByArtist>();
    public DbSet<SalesByCategory> SalesByCategory => Set<SalesByCategory>();
    public DbSet<SalesByCity> SalesByCity => Set<SalesByCity>();
    public DbSet<ProductRating> ProductRatings => Set<ProductRating>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleID);
            e.Property(x => x.RoleName).HasMaxLength(20);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleID);
        });

        b.Entity<City>(e =>
        {
            e.ToTable("Cities");
            e.HasKey(x => x.CityID);
        });

        b.Entity<Address>(e =>
        {
            e.ToTable("Addresses");
            e.HasKey(x => x.AddressID);
            e.HasOne(x => x.User).WithMany(u => u.Addresses).HasForeignKey(x => x.UserID);
            e.HasOne(x => x.City).WithMany(c => c.Addresses).HasForeignKey(x => x.CityID);
        });

        b.Entity<Artist>(e =>
        {
            e.ToTable("Artists");
            e.HasKey(x => x.ArtistID);
            e.Property(x => x.JoinDate).HasColumnType("date");
            e.HasOne(x => x.User).WithOne(u => u.Artist).HasForeignKey<Artist>(x => x.UserID);
            e.HasOne(x => x.City).WithMany(c => c.Artists).HasForeignKey(x => x.CityID);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
        });

        b.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.ProductID);
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.HasOne(x => x.Artist).WithMany(a => a.Products).HasForeignKey(x => x.ArtistID);
            e.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryID);
        });

        b.Entity<ProductImage>(e =>
        {
            e.ToTable("ProductImages");
            e.HasKey(x => x.ImageID);
            e.HasOne(x => x.Product).WithMany(p => p.Images).HasForeignKey(x => x.ProductID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CustomRequest>(e =>
        {
            e.ToTable("CustomRequests");
            e.HasKey(x => x.RequestID);
            e.Property(x => x.Budget).HasPrecision(10, 2);
            e.Property(x => x.QuotedPrice).HasPrecision(10, 2);
            e.Property(x => x.RequiredBy).HasColumnType("date");
            e.HasOne(x => x.User).WithMany(u => u.CustomRequests).HasForeignKey(x => x.UserID);
            e.HasOne(x => x.Artist).WithMany(a => a.CustomRequests).HasForeignKey(x => x.ArtistID);
            e.HasOne(x => x.Category).WithMany(c => c.CustomRequests).HasForeignKey(x => x.CategoryID);
        });

        b.Entity<CartItem>(e =>
        {
            e.ToTable("CartItems");
            e.HasKey(x => new { x.UserID, x.ProductID });
            e.HasOne(x => x.User).WithMany(u => u.CartItems).HasForeignKey(x => x.UserID);
            e.HasOne(x => x.Product).WithMany(p => p.CartItems).HasForeignKey(x => x.ProductID);
        });

        b.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.OrderID);
            e.Property(x => x.ShippingFee).HasPrecision(10, 2);
            e.HasOne(x => x.User).WithMany(u => u.Orders).HasForeignKey(x => x.UserID);
            e.HasOne(x => x.Address).WithMany(a => a.Orders).HasForeignKey(x => x.AddressID);
        });

        b.Entity<OrderItem>(e =>
        {
            // Declaring the trigger stops EF from using an OUTPUT clause, which SQL Server
            // does not allow on tables that have triggers.
            e.ToTable("OrderItems", t => t.HasTrigger("trg_OrderItems_ReduceStock"));
            e.HasKey(x => new { x.OrderID, x.ProductID });
            e.Property(x => x.UnitPrice).HasPrecision(10, 2);
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderID);
            e.HasOne(x => x.Product).WithMany(p => p.OrderItems).HasForeignKey(x => x.ProductID);
        });

        b.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.HasKey(x => x.PaymentID);
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.HasOne(x => x.Order).WithOne(o => o.Payment).HasForeignKey<Payment>(x => x.OrderID);
        });

        b.Entity<Shipment>(e =>
        {
            e.ToTable("Shipments");
            e.HasKey(x => x.ShipmentID);
            e.HasOne(x => x.Order).WithMany(o => o.Shipments).HasForeignKey(x => x.OrderID);
            e.HasOne(x => x.Artist).WithMany(a => a.Shipments).HasForeignKey(x => x.ArtistID);
        });

        b.Entity<Review>(e =>
        {
            e.ToTable("Reviews");
            e.HasKey(x => x.ReviewID);
            e.HasOne(x => x.Product).WithMany(p => p.Reviews).HasForeignKey(x => x.ProductID);
            e.HasOne(x => x.User).WithMany(u => u.Reviews).HasForeignKey(x => x.UserID);
        });

        // ----- Views -----
        b.Entity<OrderTotal>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_OrderTotals");
            e.Property(x => x.ItemsTotal).HasPrecision(12, 2);
            e.Property(x => x.ShippingFee).HasPrecision(10, 2);
            e.Property(x => x.TotalAmount).HasPrecision(12, 2);
        });
        b.Entity<SalesByArtist>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_SalesByArtist");
            e.Property(x => x.TotalSales).HasPrecision(12, 2);
        });
        b.Entity<SalesByCategory>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_SalesByCategory");
            e.Property(x => x.TotalSales).HasPrecision(12, 2);
        });
        b.Entity<SalesByCity>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_SalesByCity");
            e.Property(x => x.TotalSales).HasPrecision(12, 2);
        });
        b.Entity<ProductRating>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_ProductRatings");
            e.Property(x => x.AvgRating).HasPrecision(4, 2);
        });

        // The database decides what happens on delete; EF should never cascade on its own
        // (except product images, which the schema also cascades).
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            if (fk.DeleteBehavior == DeleteBehavior.Cascade && fk.DeclaringEntityType.ClrType != typeof(ProductImage))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
