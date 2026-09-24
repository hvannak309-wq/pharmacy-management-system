using pharmacy.DTOs;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Services
{
    public class BatchService : IBatchService
    {
        private readonly IBatchRepository _batches;
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _uow;

        public BatchService(IBatchRepository batches, IProductRepository products, IUnitOfWork uow)
        {
            _batches = batches;
            _products = products;
            _uow = uow;
        }

        public async Task<Result<List<Batch>>> GetAllAsync()
        {
            var all = await _batches.GetAllAsync();
            return Result<List<Batch>>.Success(all);
        }

        public async Task<Result<List<Batch>>> GetExpiryAlertsAsync()
        {
            var today = DateTime.Today;
            var expired = await _batches.GetExpiredAsync(today);
            var soon30 = await _batches.GetExpiringAsync(today, today.AddDays(30));
            var soon60 = await _batches.GetExpiringAsync(today.AddDays(31), today.AddDays(60));
            var soon90 = await _batches.GetExpiringAsync(today.AddDays(61), today.AddDays(90));
            return Result<List<Batch>>.Success(expired.Concat(soon30).Concat(soon60).Concat(soon90).ToList());
        }

        public async Task<Result<int>> DisposeAsync(int batchId)
        {
            var batch = await _batches.GetByIdAsync(batchId);
            if (batch is null) return Result<int>.Failure("Batch not found.");
            batch.IsDisposed = true;
            _batches.Update(batch);
            await _uow.SaveChangesAsync();
            return Result<int>.Success(batchId);
        }

        public async Task<Result<int>> FlagExpiredAsync()
        {
            var today = DateTime.Today;
            var expired = await _batches.GetExpiredAsync(today);
            foreach (var b in expired) b.IsDisposed = true;
            await _uow.SaveChangesAsync();
            return Result<int>.Success(expired.Count);
        }
    }

    public class ReportService : IReportService
    {
        private readonly ISaleRepository _sales;
        private readonly IProductRepository _products;

        public ReportService(ISaleRepository sales, IProductRepository products)
        {
            _sales = sales;
            _products = products;
        }

        public async Task<Result<List<Sale>>> GetSalesReportAsync(DateTime from, DateTime to)
            => Result<List<Sale>>.Success(await _sales.GetByDateRangeAsync(from, to.Date.AddDays(1).AddTicks(-1)));

        public async Task<Result<List<Product>>> GetStockReportAsync()
            => Result<List<Product>>.Success(await _products.GetWithStockAsync());

        public async Task<Result<List<ProfitRow>>> GetProfitReportAsync(DateTime from, DateTime to)
        {
            var sales = await _sales.GetByDateRangeAsync(from, to.Date.AddDays(1).AddTicks(-1));
            var rows = sales
                .SelectMany(s => s.Details)
                .GroupBy(d => d.ProductId)
                .Select(g =>
                {
                    var product = g.First().Product;
                    return new ProfitRow
                    {
                        ProductId = g.Key,
                        ProductName = product?.Name ?? $"#{g.Key}",
                        QuantitySold = g.Sum(d => d.Quantity),
                        Revenue = g.Sum(d => d.Subtotal),
                        Cost = g.Sum(d => d.Quantity * (d.Batch?.PurchasePrice ?? 0))
                    };
                })
                .OrderBy(r => r.ProductName)
                .ToList();
            return Result<List<ProfitRow>>.Success(rows);
        }
    }

    public class DashboardService : IDashboardService
    {
        private readonly ISaleRepository _sales;
        private readonly IProductRepository _products;
        private readonly IBatchRepository _batches;

        public DashboardService(ISaleRepository sales, IProductRepository products, IBatchRepository batches)
        {
            _sales = sales;
            _products = products;
            _batches = batches;
        }

        public async Task<Result<DashboardDto>> GetAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var sales = await _sales.GetByDateRangeAsync(today, tomorrow.AddTicks(-1));
            var products = await _products.GetWithStockAsync();
            var expiring = await _batches.GetExpiringAsync(today, today.AddDays(90));
            var expired = await _batches.GetExpiredAsync(today);

            return Result<DashboardDto>.Success(new DashboardDto
            {
                TodaySales = sales.Sum(s => s.TotalAmount),
                TodaySaleCount = sales.Count,
                LowStockCount = products.Count(p => p.Stock <= p.ReorderLevel),
                ExpiringSoonCount = expiring.Count,
                ExpiredCount = expired.Count
            });
        }
    }
}
