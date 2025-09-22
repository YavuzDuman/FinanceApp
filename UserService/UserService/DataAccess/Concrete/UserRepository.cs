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
		public Task<List<User>> GetAllWithRolesAsync(CancellationToken ct = default)
			=> _context.Users
				  .Include(u => u.UserRoles)
				  .ThenInclude(ur => ur.Role)
				  .ToListAsync(ct);

		public Task<User?> GetByIdWithRolesAsync(int id, CancellationToken ct = default)
			=> _context.Users
				  .Include(u => u.UserRoles)
				  .ThenInclude(ur => ur.Role)
				  .FirstOrDefaultAsync(u => u.UserId == id, ct);

	}
}
