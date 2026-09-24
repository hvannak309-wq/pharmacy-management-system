using Microsoft.EntityFrameworkCore;
using pharmacy.Data;
using pharmacy.DTOs;
using pharmacy.Enums;
using pharmacy.Interfaces;
using pharmacy.Models;
using pharmacy.Repositories;
using pharmacy.Services;

namespace PharmacyMS.Tests.Services
{
    public class SaleServiceTests
    {
        private static AppDbContext NewDb(string name)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new AppDbContext(options);
        }

        private static async Task<(SaleService svc, AppDbContext db, Product product)> SetupAsync(string dbName)
        {
            var db = NewDb(dbName);
            var cat = new Category { Name = "Test" };
            db.Categories.Add(cat);
            var product = new Product
            {
                Category = cat,
                Name = "TestDrug",
                UnitPrice = 10m,
                CostPrice = 5m,
                ReorderLevel = 1
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.Batches.AddRange(
                new Batch { ProductId = product.Id, BatchNumber = "LATE", ExpiryDate = DateTime.Today.AddMonths(6), Quantity = 50, PurchasePrice = 4m },
                new Batch { ProductId = product.Id, BatchNumber = "EARLY", ExpiryDate = DateTime.Today.AddMonths(1), Quantity = 30, PurchasePrice = 5m },
                new Batch { ProductId = product.Id, BatchNumber = "EXPIRED", ExpiryDate = DateTime.Today.AddDays(-1), Quantity = 100, PurchasePrice = 3m },
                new Batch { ProductId = product.Id, BatchNumber = "DISPOSED", ExpiryDate = DateTime.Today.AddDays(-10), Quantity = 100, PurchasePrice = 3m, IsDisposed = true }
            );
            await db.SaveChangesAsync();

            var svc = new SaleService(
                new SaleRepository(db),
                new ProductRepository(db),
                new BatchRepository(db),
                new UnitOfWork(db));
            return (svc, db, product);
        }

        [Fact]
        public async Task Checkout_AllocatesFefo_EarliestExpiryFirst()
        {
            var (svc, db, product) = await SetupAsync(Guid.NewGuid().ToString());

            var result = await svc.CheckoutAsync(new CheckoutDto
            {
                UserId = 1,
                PaidAmount = 50m,
                Lines = { new CheckoutLineDto { ProductId = product.Id, Quantity = 40, UnitPrice = 10m } }
            });

            Assert.True(result.IsSuccess, result.Error);
            var sale = await db.Sales.Include(s => s.Details).FirstAsync(s => s.Id == result.Value);
            var early = await db.Batches.FirstAsync(b => b.BatchNumber == "EARLY");
            var late = await db.Batches.FirstAsync(b => b.BatchNumber == "LATE");
            var expired = await db.Batches.FirstAsync(b => b.BatchNumber == "EXPIRED");

            Assert.Equal(0, early.Quantity);
            Assert.Equal(40, late.Quantity);
            Assert.Equal(100, expired.Quantity);
            Assert.Equal(2, sale.Details.Count);
            Assert.Equal(400m, sale.TotalAmount);
        }

        [Fact]
        public async Task Checkout_SkipsExpiredAndDisposedBatches()
        {
            var (svc, db, product) = await SetupAsync(Guid.NewGuid().ToString());

            var result = await svc.CheckoutAsync(new CheckoutDto
            {
                UserId = 1,
                PaidAmount = 10m,
                Lines = { new CheckoutLineDto { ProductId = product.Id, Quantity = 1, UnitPrice = 10m } }
            });

            Assert.True(result.IsSuccess, result.Error);
            var detail = await db.SaleDetails.Include(d => d.Batch).FirstAsync(d => d.SaleId == result.Value);
            Assert.NotEqual("EXPIRED", detail.Batch!.BatchNumber);
            Assert.NotEqual("DISPOSED", detail.Batch!.BatchNumber);
        }

        [Fact]
        public async Task Checkout_InsufficientStock_Rejected()
        {
            var (svc, _, product) = await SetupAsync(Guid.NewGuid().ToString());

            var result = await svc.CheckoutAsync(new CheckoutDto
            {
                UserId = 1,
                PaidAmount = 1000m,
                Lines = { new CheckoutLineDto { ProductId = product.Id, Quantity = 1000, UnitPrice = 10m } }
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("Insufficient", result.Error);
        }

        [Fact]
        public async Task Checkout_AppliesDiscount()
        {
            var (svc, db, product) = await SetupAsync(Guid.NewGuid().ToString());

            var result = await svc.CheckoutAsync(new CheckoutDto
            {
                UserId = 1,
                Discount = 5m,
                PaidAmount = 15m,
                Lines = { new CheckoutLineDto { ProductId = product.Id, Quantity = 2, UnitPrice = 10m } }
            });

            Assert.True(result.IsSuccess, result.Error);
            var sale = await db.Sales.FirstAsync(s => s.Id == result.Value);
            Assert.Equal(15m, sale.TotalAmount);
            Assert.Equal(5m, sale.Discount);
        }
    }

    public class BatchServiceTests
    {
        private static AppDbContext NewDb(string name)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetExpiryAlerts_FlagsExpiredAndWindows()
        {
            var db = NewDb(Guid.NewGuid().ToString());
            var cat = new Category { Name = "T" };
            db.Categories.Add(cat);
            var p = new Product { Category = cat, Name = "D", UnitPrice = 1, CostPrice = 1 };
            db.Products.Add(p);
            await db.SaveChangesAsync();

            db.Batches.AddRange(
                new Batch { ProductId = p.Id, BatchNumber = "E", ExpiryDate = DateTime.Today.AddDays(-5), Quantity = 10, PurchasePrice = 1 },
                new Batch { ProductId = p.Id, BatchNumber = "B30", ExpiryDate = DateTime.Today.AddDays(20), Quantity = 10, PurchasePrice = 1 },
                new Batch { ProductId = p.Id, BatchNumber = "B90", ExpiryDate = DateTime.Today.AddDays(80), Quantity = 10, PurchasePrice = 1 },
                new Batch { ProductId = p.Id, BatchNumber = "OK", ExpiryDate = DateTime.Today.AddDays(200), Quantity = 10, PurchasePrice = 1 },
                new Batch { ProductId = p.Id, BatchNumber = "ZERO", ExpiryDate = DateTime.Today.AddDays(10), Quantity = 0, PurchasePrice = 1 }
            );
            await db.SaveChangesAsync();

            var svc = new BatchService(new BatchRepository(db), new ProductRepository(db), new UnitOfWork(db));
            var result = await svc.GetExpiryAlertsAsync();

            Assert.True(result.IsSuccess);
            var numbers = result.Value!.Select(b => b.BatchNumber).ToList();
            Assert.Contains("E", numbers);
            Assert.Contains("B30", numbers);
            Assert.Contains("B90", numbers);
            Assert.DoesNotContain("OK", numbers);
            Assert.DoesNotContain("ZERO", numbers);
        }

        [Fact]
        public async Task Dispose_MarksBatch()
        {
            var db = NewDb(Guid.NewGuid().ToString());
            var cat = new Category { Name = "T" };
            db.Categories.Add(cat);
            var p = new Product { Category = cat, Name = "D", UnitPrice = 1, CostPrice = 1 };
            db.Products.Add(p);
            await db.SaveChangesAsync();
            var batch = new Batch { ProductId = p.Id, BatchNumber = "X", ExpiryDate = DateTime.Today.AddDays(30), Quantity = 5, PurchasePrice = 1 };
            db.Batches.Add(batch);
            await db.SaveChangesAsync();

            var svc = new BatchService(new BatchRepository(db), new ProductRepository(db), new UnitOfWork(db));
            var result = await svc.DisposeAsync(batch.Id);

            Assert.True(result.IsSuccess);
            var reloaded = await db.Batches.FindAsync(batch.Id);
            Assert.True(reloaded!.IsDisposed);
        }
    }
}
