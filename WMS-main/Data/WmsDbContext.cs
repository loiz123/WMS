using Microsoft.EntityFrameworkCore;
using WMS.API.Models;

namespace WMS.API.Data;

public class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options) { }

    // Khai báo 5 bảng
    public DbSet<AppUser>         Users            { get; set; }
    public DbSet<Supplier>        Suppliers        { get; set; }
    public DbSet<Product>         Products         { get; set; }
    public DbSet<Transaction>     Transactions     { get; set; }
    public DbSet<TransactionItem> TransactionItems { get; set; }
    public DbSet<StockTake> StockTakes { get; set; }
    public DbSet<StockTakeItem> StockTakeItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tên bảng theo đúng ERD báo cáo (uppercase)
        modelBuilder.Entity<AppUser>().ToTable("APP_USERS");
        modelBuilder.Entity<Supplier>().ToTable("SUPPLIERS");
        modelBuilder.Entity<Product>().ToTable("PRODUCTS");
        modelBuilder.Entity<Transaction>().ToTable("TRANSACTIONS");
        modelBuilder.Entity<TransactionItem>().ToTable("TRANSACTION_ITEMS");
        modelBuilder.Entity<StockTake>().ToTable("STOCK_TAKES");
        modelBuilder.Entity<StockTakeItem>().ToTable("STOCK_TAKE_ITEMS");
        // Username phải unique (FR-004)
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Quan hệ: Product → Supplier (nhiều product - 1 supplier)
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);  // Xóa supplier → supplier_id = NULL

        // Quan hệ: Transaction → AppUser
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ: TransactionItem → Transaction (Master-Detail)
        modelBuilder.Entity<TransactionItem>()
            .HasOne(i => i.Transaction)
            .WithMany(t => t.Items)
            .HasForeignKey(i => i.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);  // Xóa transaction → xóa luôn items

        // Quan hệ: TransactionItem → Product
        modelBuilder.Entity<TransactionItem>()
            .HasOne(i => i.Product)
            .WithMany(p => p.TransactionItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict); // Không cho xóa product nếu có giao dịch
      
        modelBuilder.Entity<StockTake>()
            .HasOne(s => s.Creator)
            .WithMany()
            .HasForeignKey(s => s.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTake>()
            .HasOne(s => s.Approver)
            .WithMany()
            .HasForeignKey(s => s.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTakeItem>()
            .HasOne(i => i.StockTake)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.StockTakeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockTakeItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── SEED DATA: tạo tài khoản admin mặc định ──
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser {
                Id           = "U001",
                Username     = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role         = "Admin",
                FullName     = "Quản trị viên",
                CreatedAt    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AppUser {
                Id           = "U002",
                Username     = "staff",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                Role         = "Staff",
                FullName     = "Nhân viên kho",
                CreatedAt    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}