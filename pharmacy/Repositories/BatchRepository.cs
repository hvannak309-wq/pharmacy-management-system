using Microsoft.EntityFrameworkCore;
using pharmacy.Data;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Repositories
{
    public class BatchRepository : Repository<Batch>, IBatchRepository
    {
        public BatchRepository(AppDbContext db) : base(db) { }

        public Task<List<Batch>> GetByProductAsync(int productId)
            => _set.Where(b => b.ProductId == productId).OrderBy(b => b.ExpiryDate).ToListAsync();

        public Task<List<Batch>> GetFefoCandidatesAsync(int productId, DateTime today)
            => _set.Where(b => b.ProductId == productId && !b.IsDisposed && b.Quantity > 0 && b.ExpiryDate >= today)
                .OrderBy(b => b.ExpiryDate)
                .ThenBy(b => b.Id)
                .ToListAsync();

        public Task<List<Batch>> GetExpiringAsync(DateTime from, DateTime to)
            => _set.Include(b => b.Product)
                .Where(b => !b.IsDisposed && b.Quantity > 0 && b.ExpiryDate >= from && b.ExpiryDate <= to)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();

        public Task<List<Batch>> GetExpiredAsync(DateTime today)
            => _set.Include(b => b.Product)
                .Where(b => !b.IsDisposed && b.Quantity > 0 && b.ExpiryDate < today)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();

        public async Task<int> GetTotalStockAsync(int productId)
            => await _set.Where(b => b.ProductId == productId).SumAsync(b => (int?)b.Quantity) ?? 0;
    }
}
