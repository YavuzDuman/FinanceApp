using System.Linq.Expressions;

namespace StockService.DataAccess.Abstract
{
	public interface IGenericRepository<T> where T : class
	{
		Task<T> GetAsync(Expression<Func<T, bool>> filter);
		Task<List<T>> GetAllAsync();
		Task AddAsync(T entity);
		Task AddRangeAsync(IEnumerable<T> entities);
		Task UpdateAsync(T entity);
		Task DeleteAsync(T entity);
	}
}
