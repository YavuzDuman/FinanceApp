using UserService.Entities.Concrete;

namespace UserService.DataAccess.Abstract
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
        Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default);
        Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default);
    }
}
