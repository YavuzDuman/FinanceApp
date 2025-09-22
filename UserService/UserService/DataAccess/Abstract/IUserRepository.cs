using UserService.DataAccess.Concrete;
using UserService.Entities.Concrete;

namespace UserService.DataAccess.Abstract
{
	public interface IUserRepository :IRepository<User>
	{
		Task<List<User>> GetAllWithRolesAsync(CancellationToken ct = default);
		Task<User?> GetByIdWithRolesAsync(int id, CancellationToken ct = default);
	}
}
