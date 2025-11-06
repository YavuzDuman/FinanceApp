using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using UserService.Business.Abstract;
using UserService.Entities.Dto;
using WebApi.Helpers.Jwt;
using UserService.Helpers.Redis;
using Microsoft.Extensions.Configuration;

namespace UserService.Controller
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthManager _authManager;
		private readonly JwtTokenGenerator _jwtTokenGenerator;

		public AuthController(IAuthManager authManager, JwtTokenGenerator jwtTokenGenerator)
		{
			_authManager = authManager;
			_jwtTokenGenerator = jwtTokenGenerator;
		}

	[HttpPost("register")]
	public async Task<IActionResult> Register(RegisterDto registerUser, CancellationToken ct)
	{
		try
		{
			var ok = await _authManager.RegisterUserAsync(registerUser, ct);
			if (!ok) return BadRequest(new { message = "Bu kullanıcı adı veya e-posta zaten kayıtlı." });

			return Ok(new { message = "Kullanıcı başarıyla kaydedildi." });
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Register hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			return StatusCode(500, new { message = "Kayıt başarısız oldu. Lütfen tekrar deneyin.", error = ex.Message });
		}
	}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDto loginUser, CancellationToken ct)
		{
			var user = await _authManager.LoginUserAsync(loginUser, ct);
			if (user == null)
			{
				return Unauthorized(new { message = "Kullanıcı adı veya şifre yanlış." });
			}
			
			// Access Token oluştur
			var accessToken = _jwtTokenGenerator.GenerateToken(user);

			// Refresh Token oluştur
			var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
			var refreshToken = await _authManager.CreateRefreshTokenAsync(user.UserId, ipAddress, ct);

			Console.WriteLine($"Generated Access Token: {accessToken}");
			Console.WriteLine($"Generated Refresh Token: {refreshToken.Token}");

			return Ok(new AuthResponseDto
			{
				AccessToken = accessToken,
				RefreshToken = refreshToken.Token,
				UserId = user.UserId,
				Username = user.Username,
				Email = user.Email
			});
		}

		[HttpPost("refresh")]
		public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
		{
			// Refresh token'ı doğrula
			var user = await _authManager.ValidateRefreshTokenAsync(request.RefreshToken, ct);
			if (user == null)
			{
				return Unauthorized(new { message = "Geçersiz veya süresi dolmuş refresh token." });
			}

			// Eski refresh token'ı iptal et (Refresh Token Rotation)
			var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
			await _authManager.RevokeRefreshTokenAsync(request.RefreshToken, ipAddress, ct);

			// Yeni access ve refresh token oluştur
			var newAccessToken = _jwtTokenGenerator.GenerateToken(user);
			var newRefreshToken = await _authManager.CreateRefreshTokenAsync(user.UserId, ipAddress, ct);

			return Ok(new AuthResponseDto
			{
				AccessToken = newAccessToken,
				RefreshToken = newRefreshToken.Token,
				UserId = user.UserId,
				Username = user.Username,
				Email = user.Email
			});
		}

		[HttpPost("revoke")]
		[Authorize]
		public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
		{
			var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
			await _authManager.RevokeRefreshTokenAsync(request.RefreshToken, ipAddress, ct);
			return Ok(new { message = "Refresh token başarıyla iptal edildi." });
		}
	}
}
