using Microsoft.EntityFrameworkCore;
using pharmacy.Data;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext db) : base(db) { }

        public Task<Product?> GetByBarcodeAsync(string barcode)
            => _set.Include(p => p.Batches).FirstOrDefaultAsync(p => p.Barcode == barcode);

        public Task<List<Product>> SearchAsync(string term)
        {
            var t = $"%{term}%";
            return _set.Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => EF.Functions.Like(p.Name, t) || (p.Barcode != null && EF.Functions.Like(p.Barcode, t)))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public Task<List<Product>> GetWithStockAsync()
            => _set.Include(p => p.Category).Include(p => p.Batches).OrderBy(p => p.Name).ToListAsync();
    }
}
