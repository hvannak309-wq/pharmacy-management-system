using Microsoft.EntityFrameworkCore;
using pharmacy.Enums;
using pharmacy.Models;

namespace pharmacy.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<PurchaseDetail> PurchaseDetails => Set<PurchaseDetail>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
        public DbSet<Batch> Batches => Set<Batch>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Category>(e =>
            {
                e.ToTable("tblCategory");
                e.Property(x => x.Id).HasColumnName("CategoryID");
                e.Property(x => x.Name).HasColumnName("CategoryName").HasMaxLength(100).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);
                e.HasMany(x => x.Products).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId);
            });

            mb.Entity<Product>(e =>
            {
                e.ToTable("tblProduct");
                e.Property(x => x.Id).HasColumnName("ProductID");
                e.Property(x => x.CategoryId).HasColumnName("CategoryID");
                e.Property(x => x.Name).HasColumnName("ProductName").HasMaxLength(200).IsRequired();
                e.Property(x => x.Barcode).HasMaxLength(100);
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                e.Property(x => x.CostPrice).HasColumnType("decimal(18,2)");
                e.Ignore(x => x.Stock);
                e.HasIndex(x => x.Barcode);
                e.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId);
                e.HasMany(x => x.Batches).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
            });

            mb.Entity<Supplier>(e =>
            {
                e.ToTable("tblSupplier");
                e.Property(x => x.Id).HasColumnName("SupplierID");
                e.Property(x => x.Name).HasColumnName("SupplierName").HasMaxLength(200).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(50);
                e.Property(x => x.Email).HasMaxLength(100);
                e.Property(x => x.Address).HasMaxLength(300);
                e.HasMany(x => x.Purchases).WithOne(x => x.Supplier).HasForeignKey(x => x.SupplierId);
            });

            mb.Entity<Customer>(e =>
            {
                e.ToTable("tblCustomer");
                e.Property(x => x.Id).HasColumnName("CusID");
                e.Property(x => x.Name).HasColumnName("CusName").HasMaxLength(200).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(50);
                e.Property(x => x.Email).HasMaxLength(100);
                e.Property(x => x.Address).HasMaxLength(300);
                e.HasMany(x => x.Sales).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);
            });

            mb.Entity<User>(e =>
            {
                e.ToTable("tblUser");
                e.Property(x => x.Id).HasColumnName("UserID");
                e.Property(x => x.Username).HasMaxLength(50).IsRequired();
                e.HasIndex(x => x.Username).IsUnique();
                e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
                e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                e.Property(x => x.Role).HasConversion<int>();
                e.HasMany(x => x.Sales).WithOne(x => x.User).HasForeignKey(x => x.UserId);
                e.HasMany(x => x.Purchases).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            });

            mb.Entity<Purchase>(e =>
            {
                e.ToTable("tblPurchase");
                e.Property(x => x.Id).HasColumnName("PurchaseID");
                e.Property(x => x.SupplierId).HasColumnName("SupplierID");
                e.Property(x => x.UserId).HasColumnName("UserID");
                e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.Status).HasConversion<int>();
                e.HasOne(x => x.Supplier).WithMany(x => x.Purchases).HasForeignKey(x => x.SupplierId);
                e.HasMany(x => x.Details).WithOne(x => x.Purchase).HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<PurchaseDetail>(e =>
            {
                e.ToTable("tblPurchaseDetail");
                e.Property(x => x.Id).HasColumnName("PurchaseDetailID");
                e.Property(x => x.PurchaseId).HasColumnName("PurchaseID");
                e.Property(x => x.ProductId).HasColumnName("ProductID");
                e.Property(x => x.BatchId).HasColumnName("BatchID");
                e.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");
                e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Batch).WithMany(x => x.PurchaseDetails).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
            });

            mb.Entity<Sale>(e =>
            {
                e.ToTable("tblSale");
                e.Property(x => x.Id).HasColumnName("SaleID");
                e.Property(x => x.CustomerId).HasColumnName("CusID");
                e.Property(x => x.UserId).HasColumnName("UserID");
                e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.Discount).HasColumnType("decimal(18,2)");
                e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.PaymentMethod).HasConversion<int>();
                e.HasOne(x => x.Customer).WithMany(x => x.Sales).HasForeignKey(x => x.CustomerId);
                e.HasMany(x => x.Details).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<SaleDetail>(e =>
            {
                e.ToTable("tblSaleDetail");
                e.Property(x => x.Id).HasColumnName("SaleDetailID");
                e.Property(x => x.SaleId).HasColumnName("SaleID");
                e.Property(x => x.ProductId).HasColumnName("ProductID");
                e.Property(x => x.BatchId).HasColumnName("BatchID");
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Batch).WithMany(x => x.SaleDetails).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
            });

            mb.Entity<Batch>(e =>
            {
                e.ToTable("tblBatch");
                e.Property(x => x.Id).HasColumnName("BatchID");
                e.Property(x => x.ProductId).HasColumnName("ProductID");
                e.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
                e.Property(x => x.PurchasePrice).HasColumnType("decimal(18,2)");
                e.Ignore(x => x.IsExpired);
                e.HasIndex(x => new { x.ProductId, x.ExpiryDate });
                e.HasOne(x => x.Product).WithMany(x => x.Batches).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
