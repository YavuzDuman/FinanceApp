using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using UserService.Business.Abstract;
using UserService.Entities.Concrete;
using UserService.Entities.Dto;
using Shared.Helpers;

namespace UserService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize] // Tüm endpoint'ler için authentication gerekli
	public class UsersController : ControllerBase
	{
	private readonly IUserManager _manager;
	private readonly IAuthorizationService _authService;
	private readonly IMapper _mapper;
	private readonly IMemoryCache _cache;
	
	public UsersController(IUserManager manager, IAuthorizationService authorizationService, IMapper mapper, IMemoryCache cache)
	{
		_manager = manager;
		_authService = authorizationService;
		_mapper = mapper;
		_cache = cache;
	}


		// GET /api/users
		// Tüm kullanıcıları getirir. Sadece Admin ve Manager görebilir.
		[HttpGet]
		[Authorize(Policy = "AdminOrManager")]
		public async Task<IActionResult> GetAll(CancellationToken ct)
			=> Ok(await _manager.GetAllUsersAsync(ct));

	// GET /api/users/orderbydate
	[HttpGet("orderbydate")]
	[Authorize(Policy = "AdminOrManager")]
	public async Task<IActionResult> GetAllOrderByDate(CancellationToken ct)
		=> Ok(await _manager.GetAllUsersOrderByDateAsync(ct));

	// GET /api/users/me
	// Kullanıcının kendi profilini getirir
	[HttpGet("me")]
	public async Task<IActionResult> GetMyProfile(CancellationToken ct)
	{
		try
		{
			// Cached user ID al - performans için
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			
			var user = await _manager.GetUserByIdAsync(userId, ct);
			
			if (user == null)
			{
				return NotFound(new { message = "Kullanıcı bulunamadı." });
			}
			
			// Güvenlik için DTO kullan (Password field'ını dönderme)
			var userDto = _mapper.Map<UserDto>(user);
			return Ok(userDto);
		}
		catch (InvalidOperationException)
		{
			return Unauthorized(new { message = "Geçersiz token." });
		}
	}

	// GET /api/users/{id}
	[HttpGet("{id}")]
	public async Task<IActionResult> GetById(int id, CancellationToken ct)
	{
		var user = await _manager.GetUserByIdAsync(id, ct);
		if (user == null) return NotFound($"ID {id} ile eşleşen kullanıcı bulunamadı.");
		return Ok(user);
	}

	[HttpPost]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> Create([FromBody] User user, CancellationToken ct)
	{
		await _manager.CreateUserAsync(user, ct);
		return Ok("Kullanıcı eklendi.");
	}

	// PUT /api/users/me
	// Kullanıcının kendi profilini günceller
	[HttpPut("me")]
	public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto updateDto, CancellationToken ct)
	{
		try
		{
			// Cached user ID al - performans için
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);

			// Mevcut user entity'sini al (Password dahil)
			var existingUser = await _manager.GetUserEntityByIdAsync(userId, ct);
			if (existingUser == null)
			{
				return NotFound(new { message = "Kullanıcı bulunamadı." });
			}

			// Sadece güncellenebilir alanları değiştir
			existingUser.Name = updateDto.Name;
			existingUser.Username = updateDto.Username;
			existingUser.Email = updateDto.Email;
			// Password, InsertDate ve diğer alanlar aynı kalacak
			
			await _manager.UpdateUserAsync(userId, existingUser, ct);
			return Ok(new { message = "Profil başarıyla güncellendi." });
		}
		catch (InvalidOperationException)
		{
			return Unauthorized(new { message = "Geçersiz token." });
		}
		catch (Exception ex)
		{
			// Debug için exception mesajını logla
			Console.WriteLine($"UpdateMyProfile Error: {ex.Message}");
			Console.WriteLine($"Stack Trace: {ex.StackTrace}");
			return BadRequest(new { message = $"Profil güncellenirken bir hata oluştu: {ex.Message}" });
		}
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> Update(int id, [FromBody] UserDto updatedUserDto, CancellationToken ct)
	{
		var existingUser = await _manager.GetUserByIdAsync(id, ct);
		if (existingUser == null)
		{
			return NotFound($"ID {id} ile eşleşen kullanıcı bulunamadı.");
		}

		// Admin herkesi güncelleyebilir, diğerleri sadece kendilerini
		var authorizationResult = await _authService.AuthorizeAsync(User, existingUser, "OwnerOnly");
		if (!authorizationResult.Succeeded)
		{
			// Owner değilse Admin kontrolü yap
			var adminResult = await _authService.AuthorizeAsync(User, "AdminOnly");
			if (!adminResult.Succeeded)
			{
				return Forbid();
			}
		}

		await _manager.UpdateUserAsync(id, _mapper.Map<User>(updatedUserDto), ct);
		return Ok("Kullanıcı güncellendi.");
	}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id, CancellationToken ct)
		{
			var existingUser = await _manager.GetUserByIdAsync(id, ct);
			if (existingUser == null)
			{
				return NotFound($"ID {id} ile eşleşen kullanıcı bulunamadı.");
			}

			// Admin herkesi silebilir, diğerleri sadece kendilerini
			var authorizationResult = await _authService.AuthorizeAsync(User, existingUser, "OwnerOnly");
			if (!authorizationResult.Succeeded)
			{
				// Owner değilse Admin kontrolü yap
				var adminResult = await _authService.AuthorizeAsync(User, "AdminOnly");
				if (!adminResult.Succeeded)
				{
					return Forbid();
				}
			}

			await _manager.DeleteUserAsync(id, ct);
			return Ok("Kullanıcı silindi.");
		}

		[HttpPut("changepassword/{id}")]
		public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto passwordDto, CancellationToken ct)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					return BadRequest(ModelState);
				}

				var existingUser = await _manager.GetUserByIdAsync(id, ct);
				if (existingUser == null)
				{
					return NotFound(new { message = $"ID {id} ile eşleşen kullanıcı bulunamadı." });
				}

				bool isAdminAction = false;

				// Admin herkese şifre değiştirebilir, diğerleri sadece kendilerini
				var authorizationResult = await _authService.AuthorizeAsync(User, existingUser, "OwnerOnly");
				if (!authorizationResult.Succeeded)
				{
					// Owner değilse Admin kontrolü yap
					var adminResult = await _authService.AuthorizeAsync(User, "AdminOnly");
					if (!adminResult.Succeeded)
					{
						return Forbid();
					}
					isAdminAction = true; // Admin başkasının şifresini değiştiriyor
				}
				else
				{
					// Kullanıcı kendi şifresini değiştiriyor, eski şifre kontrolü gerekli
					if (string.IsNullOrEmpty(passwordDto.OldPassword))
					{
						return BadRequest(new { message = "Kendi şifrenizi değiştirmek için eski şifrenizi girmelisiniz." });
					}
				}

				await _manager.UpdatePasswordAsync(id, passwordDto.OldPassword, passwordDto.NewPassword, isAdminAction, ct);
				return Ok(new { message = "Şifre başarıyla değiştirildi." });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

	}
}
