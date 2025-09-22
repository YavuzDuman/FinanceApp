using AutoMapper;
using UserService.Business.Abstract;
using UserService.DataAccess.Abstract;
using UserService.Entities.Concrete;
using UserService.Entities.Dto;
using UserService.Entities.Enums;
using UserService.Helpers.Hashing;

namespace UserService.Business.Concrete
{
	public class UserManager : IUserManager
	{
		private readonly IUserRepository _userRepo;
		private readonly IMapper _mapper;
		private readonly IRepository<UserRole> _userRoleRepo;
		private readonly IRepository<Role> _roleRepo;
		private readonly PasswordHasher _passwordHasher;

		public UserManager(IUserRepository userRepository, IMapper mapper, IRepository<UserRole> userRoleRepository, IRepository<Role> roleRepository, PasswordHasher passwordHasher)
		{
			_userRepo = userRepository;
			_userRoleRepo = userRoleRepository;
			_roleRepo = roleRepository;
			_mapper = mapper;
			_passwordHasher = passwordHasher;
		}

		public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
		{
			var users = await _userRepo.GetAllWithRolesAsync(ct);
			return _mapper.Map<List<UserDto>>(users);
		}

		public async Task<List<UserDto>> GetAllUsersOrderByDateAsync(CancellationToken ct = default)
		{
			var users = await _userRepo.GetAllWithRolesAsync(ct);
			return _mapper.Map<List<UserDto>>(users.OrderByDescending(u => u.InsertDate)
												   .ToList());
		}

		public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdWithRolesAsync(id, ct);
			return user is null ? null : _mapper.Map<UserDto>(user);
		}

		public async Task CreateUserAsync(User user, CancellationToken ct = default)
		{
			user.Password = _passwordHasher.HashPassword(user.Password); // şimdilik mevcut hasher
			user.InsertDate = DateTime.Now;
			await _userRepo.AddAsync(user, ct);
		}

		public async Task UpdateUserAsync(int id, User updatedUser, CancellationToken ct = default)
			=> await _userRepo.UpdateAsync(id, updatedUser, ct);

		public async Task DeleteUserAsync(int id, CancellationToken ct = default)
			=> await _userRepo.DeleteAsync(id, ct);

		public async Task SoftDeleteUserByIdAsync(int id, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdAsync(id, ct);
			if (user is null) return;
			await _userRepo.UpdateAsync(id, user, ct);
		}
		public async Task AddUserToRoleAsync(int userId, RoleType roleType, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdWithRolesAsync(userId, ct);
			if (user == null)
				throw new Exception("Kullanıcı bulunamadı.");
			var roleName = roleType.ToString();
			var role = (await _roleRepo.GetAllAsync(ct)).FirstOrDefault(r => r.Name == roleName);
			if (role == null)
				throw new Exception($"Rol '{roleName}' bulunamadı.");
			var existingUserRole = user.UserRoles?.FirstOrDefault(ur => ur.RoleId == role.Id);
			if (existingUserRole == null)
			{
				var newUserRole = new UserRole { UserId = userId, RoleId = role.Id };
				await _userRoleRepo.AddAsync(newUserRole, ct);
			}
			else
			{
				throw new Exception($"Kullanıcı zaten '{roleName}' rolüne sahip.");
			}
		}

		public async Task RemoveUserFromRoleAsync(int userId, RoleType roleType, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdWithRolesAsync(userId, ct);
			if (user == null)
				throw new Exception("Kullanıcı bulunamadı.");

			var roleName = roleType.ToString();
			var role = (await _roleRepo.GetAllAsync(ct)).FirstOrDefault(r => r.Name == roleName);
			if (role == null)
				throw new Exception($"Rol '{roleName}' bulunamadı.");

			var userRoleToDelete = user.UserRoles?.FirstOrDefault(ur => ur.RoleId == role.Id);
			if (userRoleToDelete != null)
			{
				await _userRoleRepo.DeleteAsync(userRoleToDelete.UserId, ct);
			}
		}

		public async Task<bool> IsUserInRoleAsync(int userId, RoleType roleType, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdWithRolesAsync(userId, ct);
			if (user == null)
				return false;

			var roleName = roleType.ToString();
			return user.UserRoles?.Any(ur => ur.Role.Name == roleName) ?? false;
		}

	}
}
