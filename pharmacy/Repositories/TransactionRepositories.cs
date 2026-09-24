using Microsoft.EntityFrameworkCore;
using pharmacy.Data;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Repositories
{
    public class SaleRepository : Repository<Sale>, ISaleRepository
    {
        public SaleRepository(AppDbContext db) : base(db) { }

        public Task<Sale?> GetWithDetailsAsync(int id)
            => _set.Include(s => s.Details).ThenInclude(d => d.Product)
                .Include(s => s.Details).ThenInclude(d => d.Batch)
                .Include(s => s.Customer)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

        public Task<List<Sale>> GetByDateRangeAsync(DateTime from, DateTime to)
            => _set.Include(s => s.User)
                .Include(s => s.Details)
                .Where(s => s.SaleDate >= from && s.SaleDate <= to)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

        public Task<List<Sale>> GetRecentAsync(int count)
            => _set.Include(s => s.User)
                .OrderByDescending(s => s.SaleDate)
                .Take(count)
                .ToListAsync();
    }

    public class PurchaseRepository : Repository<Purchase>, IPurchaseRepository
    {
        public PurchaseRepository(AppDbContext db) : base(db) { }

        public Task<Purchase?> GetWithDetailsAsync(int id)
            => _set.Include(p => p.Details).ThenInclude(d => d.Product)
                .Include(p => p.Details).ThenInclude(d => d.Batch)
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

        public Task<List<Purchase>> GetRecentAsync(int count)
            => _set.Include(p => p.Supplier)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(count)
                .ToListAsync();
    }

    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext db) : base(db) { }

        public Task<User?> GetByUsernameAsync(string username)
            => _set.FirstOrDefaultAsync(u => u.Username == username);
    }
}
