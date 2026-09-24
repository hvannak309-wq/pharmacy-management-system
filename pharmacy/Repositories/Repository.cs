using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using pharmacy.Data;
using pharmacy.Interfaces;

namespace pharmacy.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _db;
        protected readonly DbSet<T> _set;

        public Repository(AppDbContext db)
        {
            _db = db;
            _set = db.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

        public virtual async Task<List<T>> GetAllAsync() => await _set.ToListAsync();

        public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
            => await _set.Where(predicate).ToListAsync();

        public virtual async Task AddAsync(T entity) => await _set.AddAsync(entity);

        public virtual async Task AddRangeAsync(IEnumerable<T> entities) => await _set.AddRangeAsync(entities);

        public virtual void Update(T entity) => _set.Update(entity);

        public virtual void Remove(T entity) => _set.Remove(entity);

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
            => predicate is null ? await _set.CountAsync() : await _set.CountAsync(predicate);
    }
}
