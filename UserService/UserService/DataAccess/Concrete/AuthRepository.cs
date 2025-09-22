using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserService.DataAccess.Abstract;
using UserService.DataAccess.Context;
using UserService.Entities.Concrete;
using UserService.Helpers.Hashing;

namespace UserService.DataAccess.Concrete
{
	public class AuthRepository : IAuthRepository
	{
		private readonly UserDatabaseContext _context;
		private readonly IMapper _mapper;
		private readonly PasswordHasher _passwordHasher;
		public AuthRepository(UserDatabaseContext context, IMapper mapper, PasswordHasher passwordHasher)
		{
			_context = context;
			_mapper = mapper;
			_passwordHasher = passwordHasher;
		}
		public async Task<User?> LoginUserAsync(User user, CancellationToken ct = default)
		{
			var LoggedUser = await _context.Users
				.FirstOrDefaultAsync(u => u.Username == user.Username, ct);

			if (LoggedUser is null)
			{
				return null;
			}

			bool isPasswordValid = _passwordHasher.VerifyPassword(user.Password, LoggedUser.Password);

			return isPasswordValid ? LoggedUser : null;
		}
		public async Task RegisterUserAsync(User user, CancellationToken ct = default)
		{
			user.Password = _passwordHasher.HashPassword(user.Password);
			user.InsertDate = DateTime.Now;

			await _context.Users.AddAsync(user, ct);
			await _context.SaveChangesAsync(ct);
		}
		public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken ct = default)
		{
			return await _context.Users.AnyAsync(u => u.Username == username || u.Email == email, ct);
		}

		public Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
		{
			return _context.Users
				.Include(u => u.UserRoles) 
				.ThenInclude(ur => ur.Role) 
				.FirstOrDefaultAsync(u => u.UserId == userId, ct);
		}
	}
}
