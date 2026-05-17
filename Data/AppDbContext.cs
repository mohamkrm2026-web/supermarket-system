using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;
using System.Security.Cryptography;
using System.Text;

namespace SuperMarket.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<AppUser> Users { get; set; }

        public static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        protected override void OnModelCreating(ModelBuilder m)
        {
            m.Entity<Product>().HasData(
                new Product { Id=1, Name="أرز بسمتي 1كجم", Barcode="001", Category="بقالة", Price=25, CostPrice=18, Stock=50, Unit="كيس" },
                new Product { Id=2, Name="زيت ذرة 1 لتر", Barcode="002", Category="بقالة", Price=30, CostPrice=22, Stock=30, Unit="زجاجة" },
                new Product { Id=3, Name="سكر 1كجم", Barcode="003", Category="بقالة", Price=15, CostPrice=10, Stock=3, MinStock=5, Unit="كيس" },
                new Product { Id=4, Name="عصير برتقال", Barcode="004", Category="مشروبات", Price=8, CostPrice=5, Stock=60, Unit="علبة" },
                new Product { Id=5, Name="مياه معدنية", Barcode="005", Category="مشروبات", Price=3, CostPrice=1.5m, Stock=100, Unit="زجاجة" },
                new Product { Id=6, Name="شامبو", Barcode="006", Category="عناية", Price=45, CostPrice=30, Stock=20, Unit="قطعة" }
            );
            m.Entity<Customer>().HasData(
                new Customer { Id=1, Name="عميل عام", Phone="", TotalPurchases=0, Points=0 },
                new Customer { Id=2, Name="أحمد محمد", Phone="0501234567", TotalPurchases=500, Points=50 }
            );
            m.Entity<Supplier>().HasData(
                new Supplier { Id=1, Name="شركة النور للتوريدات", Phone="0501111111", Email="alnour@example.com", IsActive=true, CreatedAt=new DateTime(2026,1,1) }
            );
            m.Entity<AppUser>().HasData(
                new AppUser { Id=1, Username="admin", PasswordHash=HashPassword("admin123"), FullName="المدير العام", Role="Admin", IsActive=true, CreatedAt=new DateTime(2026,1,1) },
                new AppUser { Id=2, Username="cashier", PasswordHash=HashPassword("cashier123"), FullName="الكاشير", Role="كاشير", IsActive=true, CreatedAt=new DateTime(2026,1,1) },
                new AppUser { Id=3, Username="store", PasswordHash=HashPassword("store123"), FullName="مدير المخزن", Role="مدير مخزن", IsActive=true, CreatedAt=new DateTime(2026,1,1) },
                new AppUser { Id=4, Username="viewer", PasswordHash=HashPassword("viewer123"), FullName="مشاهد", Role="مشاهدة فقط", IsActive=true, CreatedAt=new DateTime(2026,1,1) }
            );
        }
    }
}
