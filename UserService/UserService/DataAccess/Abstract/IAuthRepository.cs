using UserService.Entities.Concrete;
using UserService.Entities.Dto;

namespace UserService.DataAccess.Abstract
{
	public interface IAuthRepository
	{
		Task<User?> LoginUserAsync(User user, CancellationToken ct = default);
		Task<User> RegisterUserAsync(User user, CancellationToken ct = default);
		Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default);
		Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken ct = default);
	}
}
