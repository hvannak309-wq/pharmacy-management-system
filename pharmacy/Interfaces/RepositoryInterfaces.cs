using System.Linq.Expressions;
using pharmacy.Models;

namespace pharmacy.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetByBarcodeAsync(string barcode);
        Task<List<Product>> SearchAsync(string term);
        Task<List<Product>> GetWithStockAsync();
    }

    public interface IBatchRepository : IRepository<Batch>
    {
        Task<List<Batch>> GetByProductAsync(int productId);
        Task<List<Batch>> GetFefoCandidatesAsync(int productId, DateTime today);
        Task<List<Batch>> GetExpiringAsync(DateTime from, DateTime to);
        Task<List<Batch>> GetExpiredAsync(DateTime today);
        Task<int> GetTotalStockAsync(int productId);
    }

    public interface ISaleRepository : IRepository<Sale>
    {
        Task<Sale?> GetWithDetailsAsync(int id);
        Task<List<Sale>> GetByDateRangeAsync(DateTime from, DateTime to);
        Task<List<Sale>> GetRecentAsync(int count);
    }

    public interface IPurchaseRepository : IRepository<Purchase>
    {
        Task<Purchase?> GetWithDetailsAsync(int id);
        Task<List<Purchase>> GetRecentAsync(int count);
    }

    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
    }
}
