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
		private readonly IUserRoleRepository _userRoleRepository;
		private readonly IRepository<Role> _roleRepo;
		private readonly PasswordHasher _passwordHasher;

		public UserManager(IUserRepository userRepository, IMapper mapper, IRepository<UserRole> userRoleRepository, IUserRoleRepository userRoleRepositorySpecial, IRepository<Role> roleRepository, PasswordHasher passwordHasher)
		{
			_userRepo = userRepository;
			_userRoleRepo = userRoleRepository;
			_userRoleRepository = userRoleRepositorySpecial;
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
			// ⚡ SQL'de sıralama yapılıyor, memory'de değil
			var users = await _userRepo.GetAllWithRolesOrderByDateAsync(ct);
			return _mapper.Map<List<UserDto>>(users);
		}

		public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdWithRolesAsync(id, ct);
			return user is null ? null : _mapper.Map<UserDto>(user);
		}

		public async Task<User?> GetUserEntityByIdAsync(int id, CancellationToken ct = default)
		{
			return await _userRepo.GetByIdWithRolesAsync(id, ct);
		}

		public async Task CreateUserAsync(User user, CancellationToken ct = default)
		{
		user.Password = _passwordHasher.HashPassword(user.Password); // şimdilik mevcut hasher
		user.InsertDate = DateTime.UtcNow;
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
			user.IsActive = false; // Pasif hale getir
			await _userRepo.UpdateAsync(id, user, ct);
		}

		public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdAsync(userId, ct);
			if (user == null)
				return false;
			
			user.IsActive = isActive;
			await _userRepo.UpdateAsync(userId, user, ct);
			return true;
		}

		public async Task<bool> UpdateUserInfoAsync(int userId, string name, string email, string? username, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdAsync(userId, ct);
			if (user == null)
				return false;

			// Username değişikliği varsa ve başka bir kullanıcıda mevcut mu kontrol et
			if (!string.IsNullOrEmpty(username) && username != user.Username)
			{
				if (await _userRepo.ExistsByUsernameAsync(username, userId, ct))
					throw new Exception("Bu kullanıcı adı zaten kullanılıyor.");
				user.Username = username;
			}

			// Email değişikliği varsa ve başka bir kullanıcıda mevcut mu kontrol et
			if (email != user.Email)
			{
				if (await _userRepo.ExistsByEmailAsync(email, userId, ct))
					throw new Exception("Bu e-posta adresi zaten kullanılıyor.");
				user.Email = email;
			}
			
			user.Name = name;
			await _userRepo.UpdateAsync(userId, user, ct);
			return true;
		}

		public async Task<bool> UpdateUserPasswordAsync(int userId, string newPassword, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdAsync(userId, ct);
			if (user == null)
				return false;

			var hashedPassword = _passwordHasher.HashPassword(newPassword);
			await _userRepo.UpdatePasswordAsync(userId, hashedPassword, ct);
			return true;
		}

		public async Task<bool> UpdateUserRoleAsync(int userId, RoleType roleType, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdWithRolesAsync(userId, ct);
			if (user == null)
				return false;

			var roleName = roleType.ToString();
			var role = (await _roleRepo.GetAllAsync(ct)).FirstOrDefault(r => r.Name == roleName);
			if (role == null)
				throw new Exception($"Rol '{roleName}' bulunamadı.");

			// Mevcut tüm rolleri kaldır
			if (user.UserRoles != null && user.UserRoles.Any())
			{
				foreach (var userRole in user.UserRoles.ToList())
				{
					await _userRoleRepository.RemoveRoleFromUserAsync(userId, userRole.RoleId, ct);
				}
			}

			// Yeni rolü ekle
			await _userRoleRepository.AddRoleToUserAsync(userId, role.Id, ct);
			
			return true;
		}

		public async Task<bool> CheckUsernameExistsAsync(string username, int? excludeUserId = null, CancellationToken ct = default)
		{
			return await _userRepo.ExistsByUsernameAsync(username, excludeUserId, ct);
		}

		public async Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default)
		{
			return await _userRepo.ExistsByEmailAsync(email, excludeUserId, ct);
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

		public async Task UpdatePasswordAsync(int userId, string? oldPassword, string newPassword, bool isAdminAction, CancellationToken ct = default)
		{
			var user = await _userRepo.GetByIdAsync(userId, ct);
			if (user == null)
				throw new Exception("Kullanıcı bulunamadı");

			// Kullanıcı kendi şifresini değiştiriyorsa eski şifre kontrolü yap
			if (!isAdminAction)
			{
				if (string.IsNullOrEmpty(oldPassword))
					throw new Exception("Eski şifre gereklidir");

				if (!_passwordHasher.VerifyPassword(oldPassword, user.Password))
					throw new Exception("Eski şifre hatalı");
			}

			var hashedPassword = _passwordHasher.HashPassword(newPassword);
			await _userRepo.UpdatePasswordAsync(userId, hashedPassword, ct);
		}
	}
}
