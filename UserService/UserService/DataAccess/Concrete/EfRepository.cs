using Microsoft.EntityFrameworkCore;
using Shared.Abstract;
using UserService.DataAccess.Abstract;
using UserService.DataAccess.Context;

namespace UserService.DataAccess.Concrete
{
	public class EfRepository<T> : IRepository<T> where T : class, IEntity, new()
	{
		private readonly UserDatabaseContext _context;

		public EfRepository(UserDatabaseContext context)
		{
			_context = context;
		}
		public async Task AddAsync(T TEntity, CancellationToken ct = default)
		{
			await _context.Set<T>().AddAsync(TEntity, ct);
			await _context.SaveChangesAsync(ct);
		}

		public async Task DeleteAsync(int id, CancellationToken ct = default)
		{
			var entity = await _context.Set<T>().FindAsync([id], ct);
			if (entity is null) return;
			_context.Set<T>().Remove(entity);
			await _context.SaveChangesAsync(ct);
		}

		public Task<List<T>> GetAllAsync(CancellationToken ct = default)
		{
			return _context.Set<T>()
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.ToListAsync(ct);
		}

		public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
		{
			// FindAsync için AsNoTracking kullanılamaz, direkt Find kullan
			return await _context.Set<T>().FindAsync([id], ct);
		}

		public async Task UpdateAsync(int id, T TEntity, CancellationToken ct = default)
		{
			var existingEntity = await _context.Set<T>().FindAsync(new object[] { id }, ct);
			if (existingEntity is null) throw new Exception("Entity not found");
			_context.Entry(existingEntity).CurrentValues.SetValues(TEntity);
			await _context.SaveChangesAsync(ct);
		}
	}
}
