using UserService.Business.Abstract;
using UserService.DataAccess.Abstract;
using UserService.Entities.Concrete;

namespace UserService.Business.Concrete
{
    public class RoleManager : IRoleManager
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public RoleManager(IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
        {
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
        {
            return await _roleRepository.GetAllRolesAsync(ct);
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken ct = default)
        {
            return await _roleRepository.GetByIdAsync(roleId, ct);
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        {
            return await _roleRepository.GetRoleByNameAsync(roleName, ct);
        }

        public async Task<Role> CreateRoleAsync(string roleName, CancellationToken ct = default)
        {
            if (await _roleRepository.RoleExistsAsync(roleName, ct))
            {
                throw new InvalidOperationException($"Rol '{roleName}' zaten mevcut.");
            }

            var role = new Role
            {
                Name = roleName
            };

            await _roleRepository.AddAsync(role, ct);
            return role;
        }

        public async Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default)
        {
            return await _roleRepository.RoleExistsAsync(roleName, ct);
        }

        public async Task<List<UserRole>> GetUserRolesAsync(int userId, CancellationToken ct = default)
        {
            return await _userRoleRepository.GetUserRolesAsync(userId, ct);
        }

        public async Task AddRoleToUserAsync(int userId, string roleName, CancellationToken ct = default)
        {
            var role = await _roleRepository.GetRoleByNameAsync(roleName, ct);
            if (role == null)
            {
                throw new InvalidOperationException($"Rol '{roleName}' bulunamadı.");
            }

            if (await _userRoleRepository.UserHasRoleAsync(userId, roleName, ct))
            {
                throw new InvalidOperationException($"Kullanıcı zaten '{roleName}' rolüne sahip.");
            }

            await _userRoleRepository.AddRoleToUserAsync(userId, role.Id, ct);
        }

        public async Task RemoveRoleFromUserAsync(int userId, string roleName, CancellationToken ct = default)
        {
            var role = await _roleRepository.GetRoleByNameAsync(roleName, ct);
            if (role == null)
            {
                throw new InvalidOperationException($"Rol '{roleName}' bulunamadı.");
            }

            await _userRoleRepository.RemoveRoleFromUserAsync(userId, role.Id, ct);
        }

        public async Task<bool> UserHasRoleAsync(int userId, string roleName, CancellationToken ct = default)
        {
            return await _userRoleRepository.UserHasRoleAsync(userId, roleName, ct);
        }

        public async Task InitializeDefaultRolesAsync(CancellationToken ct = default)
        {
            var defaultRoles = new[] { "Admin", "Manager", "User", "Analyst" };

            foreach (var roleName in defaultRoles)
            {
                if (!await _roleRepository.RoleExistsAsync(roleName, ct))
                {
                    var role = new Role { Name = roleName };
                    await _roleRepository.AddAsync(role, ct);
                }
            }
        }
    }
}
