using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using UserService.Business.Abstract;
using UserService.Entities.Dto;
using Shared.Helpers;

namespace UserService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Policy = "AdminOnly")] 
	public class AdminController : ControllerBase
	{
		private readonly IUserManager _userManager;
		private readonly IMemoryCache _cache;

		public AdminController(IUserManager userManager, IMemoryCache cache)
		{
			_userManager = userManager;
			_cache = cache;
		}

		// GET /api/admin/users
		// Tüm kullanıcıları listele 
		[HttpGet("users")] 
		public async Task<IActionResult> GetAllUsers(CancellationToken ct = default)
		{
			try
			{
				var users = await _userManager.GetAllUsersOrderByDateAsync(ct);
				return Ok(users);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"GetAllUsers hatası: {ex.Message}");
				return StatusCode(500, new { message = "Kullanıcılar yüklenirken bir hata oluştu.", error = ex.Message });
			}
		}

		// GET /api/admin/users/{id}
		// Belirli bir kullanıcıyı getir
		[HttpGet("users/{id}")]
		public async Task<IActionResult> GetUserById(int id, CancellationToken ct = default)
		{
			try
			{
				var user = await _userManager.GetUserByIdAsync(id, ct);
				if (user == null)
				{
					return NotFound(new { message = $"ID {id} ile kullanıcı bulunamadı." });
				}
				return Ok(user);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"GetUserById hatası: {ex.Message}");
				return StatusCode(500, new { message = "Kullanıcı yüklenirken bir hata oluştu.", error = ex.Message });
			}
		}

		// PUT /api/admin/users/{id}/status
		// Kullanıcıyı pasif/aktif yap
		[HttpPut("users/{id}/status")]
		public async Task<IActionResult> SetUserStatus(int id, [FromBody] SetUserStatusDto statusDto, CancellationToken ct = default)
		{
			try
			{
				var success = await _userManager.SetUserActiveStatusAsync(id, statusDto.IsActive, ct);
				if (!success)
				{
					return NotFound(new { message = $"ID {id} ile kullanıcı bulunamadı." });
				}
				return Ok(new { message = $"Kullanıcı {(statusDto.IsActive ? "aktif" : "pasif")} hale getirildi." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"SetUserStatus hatası: {ex.Message}");
				return StatusCode(500, new { message = "Kullanıcı durumu güncellenirken bir hata oluştu.", error = ex.Message });
			}
		}

		// PUT /api/admin/users/{id}
		// Kullanıcı bilgilerini güncelle (name, email, username)
		[HttpPut("users/{id}")]
		public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateDto, CancellationToken ct = default)
		{
			try
			{
				var success = await _userManager.UpdateUserInfoAsync(id, updateDto.Name, updateDto.Email, updateDto.Username, ct);
				if (!success)
				{
					return NotFound(new { message = $"ID {id} ile kullanıcı bulunamadı." });
				}
				return Ok(new { message = "Kullanıcı bilgileri başarıyla güncellendi." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"UpdateUser hatası: {ex.Message}");
				return BadRequest(new { message = ex.Message });
			}
		}

		// PUT /api/admin/users/{id}/password
		// Kullanıcı şifresini güncelle
		[HttpPut("users/{id}/password")]
		public async Task<IActionResult> UpdateUserPassword(int id, [FromBody] UpdatePasswordDto passwordDto, CancellationToken ct = default)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(passwordDto.NewPassword))
				{
					return BadRequest(new { message = "Yeni şifre boş olamaz." });
				}

				var success = await _userManager.UpdateUserPasswordAsync(id, passwordDto.NewPassword, ct);
				if (!success)
				{
					return NotFound(new { message = $"ID {id} ile kullanıcı bulunamadı." });
				}
				return Ok(new { message = "Kullanıcı şifresi başarıyla güncellendi." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"UpdateUserPassword hatası: {ex.Message}");
				return StatusCode(500, new { message = "Şifre güncellenirken bir hata oluştu.", error = ex.Message });
			}
		}

		// PUT /api/admin/users/{id}/role
		// Kullanıcı rolünü güncelle
		[HttpPut("users/{id}/role")]
		public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto roleDto, CancellationToken ct = default)
		{
			try
			{
				if (!Enum.TryParse<Entities.Enums.RoleType>(roleDto.Role, true, out var roleType))
				{
					return BadRequest(new { message = $"Geçersiz rol: {roleDto.Role}" });
				}

				var success = await _userManager.UpdateUserRoleAsync(id, roleType, ct);
				if (!success)
				{
					return NotFound(new { message = $"ID {id} ile kullanıcı bulunamadı." });
				}
				return Ok(new { message = "Kullanıcı rolü başarıyla güncellendi." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"UpdateUserRole hatası: {ex.Message}");
				return StatusCode(500, new { message = "Rol güncellenirken bir hata oluştu.", error = ex.Message });
			}
		}

		// GET /api/admin/users/check-username
		// Username kontrolü
		[HttpGet("users/check-username")]
		public async Task<IActionResult> CheckUsername([FromQuery] string username, [FromQuery] int? excludeUserId = null, CancellationToken ct = default)
		{
			try
			{
				var exists = await _userManager.CheckUsernameExistsAsync(username, excludeUserId, ct);
				return Ok(new { exists });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CheckUsername hatası: {ex.Message}");
				return StatusCode(500, new { message = "Kontrol yapılırken bir hata oluştu.", error = ex.Message });
			}
		}

		// GET /api/admin/users/check-email
		// Email kontrolü
		[HttpGet("users/check-email")]
		public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] int? excludeUserId = null, CancellationToken ct = default)
		{
			try
			{
				var exists = await _userManager.CheckEmailExistsAsync(email, excludeUserId, ct);
				return Ok(new { exists });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CheckEmail hatası: {ex.Message}");
				return StatusCode(500, new { message = "Kontrol yapılırken bir hata oluştu.", error = ex.Message });
			}
		}

		// DELETE /api/admin/users/{id}
		// Kullanıcıyı pasif hale getir (soft delete)
		[HttpDelete("users/{id}")]
		public async Task<IActionResult> DeleteUser(int id, CancellationToken ct = default)
		{
			try
			{
				await _userManager.SoftDeleteUserByIdAsync(id, ct);
				return Ok(new { message = "Kullanıcı pasif hale getirildi." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"DeleteUser hatası: {ex.Message}");
				return StatusCode(500, new { message = "Kullanıcı silinirken bir hata oluştu.", error = ex.Message });
			}
		}
	}

	// DTO'lar
	public class SetUserStatusDto
	{
		public bool IsActive { get; set; }
	}

	public class UpdateUserDto
	{
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? Username { get; set; }
	}

	public class UpdatePasswordDto
	{
		public string NewPassword { get; set; } = string.Empty;
	}

	public class UpdateRoleDto
	{
		public string Role { get; set; } = string.Empty; // "Admin", "Manager", "User"
	}
}
