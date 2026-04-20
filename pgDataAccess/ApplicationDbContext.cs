using Microsoft.EntityFrameworkCore;
using pgDataAccess.Models;

namespace pgDataAccess;

public class ApplicationDbContext : DbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PickupPoint> PickupPoints => Set<PickupPoint>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public string ConnectionString { get; set; } = DatabaseConfig.ConnectionString;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Login).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Manufacturer
        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Supplier
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Article);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PhotoPath).HasMaxLength(200);
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId);
            entity.HasOne(e => e.Manufacturer)
                .WithMany(m => m.Products)
                .HasForeignKey(e => e.ManufacturerId);
            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(e => e.SupplierId);
        });

        // PickupPoint
        modelBuilder.Entity<PickupPoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(300);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Client)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.ClientId);
            entity.HasOne(e => e.PickupPoint)
                .WithMany(p => p.Orders)
                .HasForeignKey(e => e.PickupPointId);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.ProductArticle });
            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductArticle);
        });
    }
}
