using Microsoft.EntityFrameworkCore;
using StockService.DataAccess.Abstract;
using StockService.DataAccess.Context;
using System.Linq.Expressions;

namespace StockService.DataAccess.Concrete
{
	public class GenericRepository<T> : IGenericRepository<T> where T : class
	{
		protected readonly StockDbContext _context;
		private readonly DbSet<T> _dbSet;

		public GenericRepository(StockDbContext context)
		{
			_context = context;
			_dbSet = _context.Set<T>();
		}

		public async Task<T> GetAsync(Expression<Func<T, bool>> filter)
		{
			return await _dbSet
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.FirstOrDefaultAsync(filter);
		}

		public async Task<List<T>> GetAllAsync()
		{
			return await _dbSet
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.ToListAsync();
		}

		public async Task AddAsync(T entity)
		{
			await _dbSet.AddAsync(entity);
			await _context.SaveChangesAsync();
		}

		public async Task AddRangeAsync(IEnumerable<T> entities)
		{
			await _dbSet.AddRangeAsync(entities);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(T entity)
		{
			_dbSet.Update(entity);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(T entity)
		{
			_dbSet.Remove(entity);
			await _context.SaveChangesAsync();
		}
	}
}
