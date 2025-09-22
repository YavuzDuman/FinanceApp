using Shared.Abstract;

namespace UserService.DataAccess.Abstract
{
	public interface IRepository<T> where T : class, IEntity, new()
	{
		Task AddAsync(T TEntity, CancellationToken ct = default);
		Task UpdateAsync(int id,T TEntity, CancellationToken ct = default);
		Task DeleteAsync(int id, CancellationToken ct = default);
		Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
		Task<List<T>> GetAllAsync(CancellationToken ct = default);

	}
}
