using UserService.Entities.Concrete;

namespace UserService.DataAccess.Abstract
{
    public interface IUserRoleRepository : IRepository<UserRole>
    {
        Task<List<UserRole>> GetUserRolesAsync(int userId, CancellationToken ct = default);
        Task<bool> UserHasRoleAsync(int userId, string roleName, CancellationToken ct = default);
        Task AddRoleToUserAsync(int userId, int roleId, CancellationToken ct = default);
        Task RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default);
    }
}
