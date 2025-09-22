using UserService.Entities.Concrete;
using UserService.Entities.Dto;

namespace UserService.Business.Abstract
{
	public interface IAuthManager
	{
		Task<User?> LoginUserAsync(LoginDto loginUser, CancellationToken ct = default);
		Task<bool> RegisterUserAsync(RegisterDto dto, CancellationToken ct = default);
		Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default);

	}
}
