using UserService.Entities.Concrete;
using UserService.Entities.Dto;
using UserService.Entities.Enums;

namespace UserService.Business.Abstract
{
	public interface IUserManager
	{
		Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
		Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default);
		Task<User?> GetUserEntityByIdAsync(int id, CancellationToken ct = default);
		Task CreateUserAsync(User user, CancellationToken ct = default);
		Task UpdateUserAsync(int id, User updatedUser, CancellationToken ct = default);
		Task DeleteUserAsync(int id, CancellationToken ct = default);
		Task<List<UserDto>> GetAllUsersOrderByDateAsync(CancellationToken ct = default);
		Task SoftDeleteUserByIdAsync(int id, CancellationToken ct = default);
		Task AddUserToRoleAsync(int userId, RoleType roleType, CancellationToken ct = default);
		Task RemoveUserFromRoleAsync(int userId, RoleType roleType, CancellationToken ct = default);
		Task<bool> IsUserInRoleAsync(int userId, RoleType roleType, CancellationToken ct = default);
		Task UpdatePasswordAsync(int userId, string? oldPassword, string newPassword, bool isAdminAction, CancellationToken ct = default);
		Task<bool> SetUserActiveStatusAsync(int userId, bool isActive, CancellationToken ct = default);
		Task<bool> UpdateUserInfoAsync(int userId, string name, string email, string? username, CancellationToken ct = default);
		Task<bool> UpdateUserPasswordAsync(int userId, string newPassword, CancellationToken ct = default);
		Task<bool> UpdateUserRoleAsync(int userId, RoleType roleType, CancellationToken ct = default);
		Task<bool> CheckUsernameExistsAsync(string username, int? excludeUserId = null, CancellationToken ct = default);
		Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default);
	}
}
