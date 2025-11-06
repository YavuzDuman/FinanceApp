using UserService.DataAccess.Concrete;
using UserService.Entities.Concrete;

namespace UserService.DataAccess.Abstract
{
	public interface IUserRepository :IRepository<User>
	{
		Task<List<User>> GetAllWithRolesAsync(CancellationToken ct = default);
		Task<List<User>> GetAllWithRolesOrderByDateAsync(CancellationToken ct = default);
		Task<User?> GetByIdWithRolesAsync(int id, CancellationToken ct = default);

		Task UpdatePasswordAsync(int id, string newHashedPassword, CancellationToken ct = default);
		Task<bool> ExistsByUsernameAsync(string username, int? excludeUserId = null, CancellationToken ct = default);
		Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null, CancellationToken ct = default);
	}
}
