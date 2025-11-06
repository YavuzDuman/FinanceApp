using Microsoft.EntityFrameworkCore;
using UserService.DataAccess.Abstract;
using UserService.DataAccess.Context;
using UserService.Entities.Concrete;

namespace UserService.DataAccess.Concrete
{
	public class UserRepository : EfRepository<User>, IUserRepository
	{
		private readonly UserDatabaseContext _context;
		public UserRepository(UserDatabaseContext context) : base(context)
		{
			_context = context;
		}
		public async Task<List<User>> GetAllWithRolesAsync(CancellationToken ct = default)
			=> await _context.Users
				  .AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				  .Include(u => u.UserRoles)
				  .ThenInclude(ur => ur.Role)
				  .ToListAsync(ct);

		public async Task<List<User>> GetAllWithRolesOrderByDateAsync(CancellationToken ct = default)
			=> await _context.Users
				  .AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				  .Include(u => u.UserRoles)
				  .ThenInclude(ur => ur.Role)
				  .OrderByDescending(u => u.InsertDate) // ⚡ SQL'de sıralama
				  .ToListAsync(ct);

		public async Task<User?> GetByIdWithRolesAsync(int id, CancellationToken ct = default)
			=> await _context.Users
				  .AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				  .Include(u => u.UserRoles)
				  .ThenInclude(ur => ur.Role)
				  .FirstOrDefaultAsync(u => u.UserId == id, ct);

		public async Task UpdatePasswordAsync(int id, string newHashedPassword, CancellationToken ct = default)
		{
			var user = await _context.Users.FindAsync(new object[] { id }, ct);
			if (user is null) throw new Exception("User not found");
			user.Password = newHashedPassword;
			_context.Users.Update(user);
			await _context.SaveChangesAsync(ct);
		}

		public async Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null, CancellationToken ct = default)
		{
			if (excludeUserId.HasValue)
			{
				return await _context.Users.AnyAsync(u => u.Username == username && u.UserId != excludeUserId.Value, ct);
			}
			return await _context.Users.AnyAsync(u => u.Username == username, ct);
		}

		public async Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null, CancellationToken ct = default)
		{
			if (excludeUserId.HasValue)
			{
				return await _context.Users.AnyAsync(u => u.Email == email && u.UserId != excludeUserId.Value, ct);
			}
			return await _context.Users.AnyAsync(u => u.Email == email, ct);
		}

	}
}
