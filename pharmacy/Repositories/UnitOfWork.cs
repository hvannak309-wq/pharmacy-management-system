using Microsoft.EntityFrameworkCore;
using pharmacy.Data;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        public UnitOfWork(AppDbContext db) => _db = db;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                await action();
                await tx.CommitAsync();
            });
        }
    }
}
