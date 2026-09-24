using Microsoft.EntityFrameworkCore;
using pharmacy.Data;
using pharmacy.Data.Seed;
using pharmacy.Interfaces;
using pharmacy.Repositories;
using pharmacy.Services;

namespace pharmacy.Helpers
{
    public static class AppServices
    {
        public static AppDbContext Db { get; private set; } = null!;
        public static IAuthService Auth { get; private set; } = null!;
        public static ISaleService Sales { get; private set; } = null!;
        public static IPurchaseService Purchases { get; private set; } = null!;
        public static IBatchService Batches { get; private set; } = null!;
        public static IReportService Reports { get; private set; } = null!;
        public static IDashboardService Dashboard { get; private set; } = null!;
        public static IProductRepository ProductRepo { get; private set; } = null!;
        public static IRepository<Models.Category> CategoryRepo { get; private set; } = null!;
        public static IRepository<Models.Supplier> SupplierRepo { get; private set; } = null!;
        public static IRepository<Models.Customer> CustomerRepo { get; private set; } = null!;
        public static IUserRepository UserRepo { get; private set; } = null!;

        public static async Task InitAsync(string connectionString)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            Db = new AppDbContext(options);
            await Db.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(Db);

            var uow = new UnitOfWork(Db);
            var products = new ProductRepository(Db);
            var batches = new BatchRepository(Db);
            var sales = new SaleRepository(Db);
            var purchases = new PurchaseRepository(Db);
            var users = new UserRepository(Db);

            CategoryRepo = new Repository<Models.Category>(Db);
            SupplierRepo = new Repository<Models.Supplier>(Db);
            CustomerRepo = new Repository<Models.Customer>(Db);
            UserRepo = users;
            ProductRepo = products;

            Auth = new AuthService(users, uow);
            Sales = new SaleService(sales, products, batches, uow);
            Purchases = new PurchaseService(purchases, products, batches, uow);
            Batches = new BatchService(batches, products, uow);
            Reports = new ReportService(sales, products);
            Dashboard = new DashboardService(sales, products, batches);
        }
    }
}
