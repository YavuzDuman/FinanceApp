using UserService.Entities.Concrete;

namespace UserService.Business.Abstract
{
    public interface IRoleManager
    {
        Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);
        Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken ct = default);
        Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
        Task<Role> CreateRoleAsync(string roleName, CancellationToken ct = default);
        Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default);
        Task<List<UserRole>> GetUserRolesAsync(int userId, CancellationToken ct = default);
        Task AddRoleToUserAsync(int userId, string roleName, CancellationToken ct = default);
        Task RemoveRoleFromUserAsync(int userId, string roleName, CancellationToken ct = default);
        Task<bool> UserHasRoleAsync(int userId, string roleName, CancellationToken ct = default);
        Task InitializeDefaultRolesAsync(CancellationToken ct = default);
    }
}
