using pharmacy.DTOs;
using pharmacy.Enums;
using pharmacy.Models;

namespace pharmacy.Interfaces
{
    public interface IAuthService
    {
        Task<Result<User>> LoginAsync(string username, string password);
        Task<Result<int>> CreateUserAsync(User user, string password);
        Task<Result<int>> UpdateUserAsync(User user, string? newPassword);
    }

    public interface IBatchService
    {
        Task<Result<List<Batch>>> GetAllAsync();
        Task<Result<List<Batch>>> GetExpiryAlertsAsync();
        Task<Result<int>> DisposeAsync(int batchId);
        Task<Result<int>> FlagExpiredAsync();
    }

    public interface IPurchaseService
    {
        Task<Result<List<Purchase>>> GetRecentAsync(int count = 100);
        Task<Result<Purchase?>> GetByIdAsync(int id);
        Task<Result<int>> CreateAsync(PurchaseCreateDto dto);
    }

    public interface ISaleService
    {
        Task<Result<List<Sale>>> GetRecentAsync(int count = 100);
        Task<Result<Sale?>> GetByIdAsync(int id);
        Task<Result<int>> CheckoutAsync(CheckoutDto dto);
    }

    public interface IReportService
    {
        Task<Result<List<Sale>>> GetSalesReportAsync(DateTime from, DateTime to);
        Task<Result<List<Product>>> GetStockReportAsync();
        Task<Result<List<ProfitRow>>> GetProfitReportAsync(DateTime from, DateTime to);
    }

    public interface IDashboardService
    {
        Task<Result<DashboardDto>> GetAsync();
    }
}
