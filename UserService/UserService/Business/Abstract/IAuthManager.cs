using UserService.Entities.Concrete;
using UserService.Entities.Dto;

namespace UserService.Business.Abstract
{
	public interface IAuthManager
	{
		Task<User?> LoginUserAsync(LoginDto loginUser, CancellationToken ct = default);
		Task<bool> RegisterUserAsync(RegisterDto dto, CancellationToken ct = default);
		Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default);
		
		// Refresh Token metodları
		Task<RefreshToken> CreateRefreshTokenAsync(int userId, string ipAddress, CancellationToken ct = default);
		Task<User?> ValidateRefreshTokenAsync(string token, CancellationToken ct = default);
		Task RevokeRefreshTokenAsync(string token, string ipAddress, CancellationToken ct = default);
		Task RemoveExpiredRefreshTokensAsync(CancellationToken ct = default);
	}
}
