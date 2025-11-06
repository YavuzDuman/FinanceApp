using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortfolioService.Business.Abstract;
using PortfolioService.Entities.Dtos;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Shared.Extensions;
using Shared.Helpers;
using System.IdentityModel.Tokens.Jwt;

namespace PortfolioService.Controllers
{
	
	[ApiController]
	[Route("api/portfolios")]
	[Authorize] 
	public class PortfolioController : ControllerBase
	{
		private readonly IPortfolioManager _portfolioManager;
		private readonly IMemoryCache _cache;

		public PortfolioController(IPortfolioManager portfolioManager, IMemoryCache cache)
		{
			_portfolioManager = portfolioManager;
			_cache = cache;
		}

		// Merkezi UserContextHelper kullanılıyor - Shared.Helpers.UserContextHelper

		[HttpPost]
		public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioDto createDto)
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var portfolio = await _portfolioManager.CreatePortfolioAsync(userId, createDto.Name);
			return CreatedAtAction(nameof(GetPortfolioById), new { id = portfolio.Id }, portfolio);
		}

		[HttpGet]
		[Authorize(Policy = "AdminOrManager")] // Manager veya Admin tüm portföyleri görebilir
		public async Task<IActionResult> GetAllPortfolios()
		{
			// Policy ile kontrol edildiği için artık manuel rol kontrolüne gerek yok
			// Manager veya Admin tüm portföyleri görebilir
			var allPortfolios = await _portfolioManager.GetAllPortfoliosAsync();
			return Ok(allPortfolios);
		}

		[HttpGet("my-portfolios")]
		[Authorize] // Normal kullanıcılar sadece kendi portföylerini görebilir
		public async Task<IActionResult> GetMyPortfolios()
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var portfolios = await _portfolioManager.GetAllPortfoliosByUserIdAsync(userId);
			return Ok(portfolios);
		}

		[HttpGet("debug-user-info")]
		[Authorize] 
		public IActionResult GetDebugUserInfo()
		{
			var userInfo = new
			{
				IsAuthenticated = User.Identity?.IsAuthenticated,
				UserName = User.Identity?.Name,
				UserId = UserContextHelper.GetUserIdFromContext(HttpContext),
				UserIdFromToken = UserContextHelper.GetUserIdFromToken(HttpContext),
				Roles = UserContextHelper.GetUserRoles(HttpContext),
				AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
			};
			
			return Ok(userInfo);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetPortfolioById(int id)
		{
			var portfolio = await _portfolioManager.GetPortfolioByIdAsync(id);
			if (portfolio == null)
			{
				return NotFound();
			}
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			if (portfolio.UserId != userId) return Forbid();
			return Ok(portfolio);
		}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdatePortfolioName(int id, [FromBody] UpdatePortfolioNameDto updateDto)
	{
		try
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var portfolio = await _portfolioManager.GetPortfolioByIdAsync(id);
			if (portfolio == null || portfolio.UserId != userId)
			{
				return Forbid();
			}

			await _portfolioManager.UpdatePortfolioNameAsync(id, updateDto.NewName);
			return NoContent();
		}
		catch (InvalidOperationException ex)
		{
			return NotFound(new { message = ex.Message });
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = "Portföy ismi güncellenirken bir hata oluştu." });
		}
	}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeletePortfolio(int id)
		{
			var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
			var portfolio = await _portfolioManager.GetPortfolioByIdAsync(id);
			if (portfolio == null || portfolio.UserId != userId)
			{
				return Forbid();
			}

			await _portfolioManager.DeletePortfolioAsync(id);
			return NoContent();
		}

		// --- Portföy Varlık (Item) Uç Noktaları ---

		[HttpPost("{portfolioId}/items")]
		public async Task<IActionResult> AddItemToPortfolio(int portfolioId, [FromBody] AddItemToPortfolioDto itemDto)
		{
			try
			{
				// Güvenlik: Portföyün kullanıcıya ait olduğunu doğrula
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var portfolio = await _portfolioManager.GetPortfolioByIdAsync(portfolioId);
				if (portfolio == null || portfolio.UserId != userId)
				{
					return Forbid();
				}

				var addedItem = await _portfolioManager.AddItemToPortfolioAsync(
					portfolioId,
					itemDto.Symbol,
					itemDto.PurchasePrice,
					itemDto.Quantity,
					HttpContext
				);
			return Ok(addedItem);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = "Hisse eklenirken beklenmeyen bir hata oluştu." });
		}
	}

		[HttpDelete("{portfolioId}/items/{itemId}")]
		public async Task<IActionResult> DeletePortfolioItem(int portfolioId, int itemId)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var portfolio = await _portfolioManager.GetPortfolioByIdAsync(portfolioId);
				if (portfolio == null || portfolio.UserId != userId)
				{
					return Forbid();
				}
				await _portfolioManager.DeletePortfolioItemAsync(itemId);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				return NotFound(ex.Message);
			}
		}

		[HttpPut("{portfolioId}/items/{itemId}")]
		public async Task<IActionResult> UpdatePortfolioItem(int portfolioId, int itemId, [FromBody] UpdatePortfolioItemDto updateDto)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var portfolio = await _portfolioManager.GetPortfolioByIdAsync(portfolioId);
				if (portfolio == null || portfolio.UserId != userId)
				{
					return Forbid();
				}
				var updatedItem = await _portfolioManager.UpdatePortfolioItemAsync(itemId, updateDto.NewQuantity, updateDto.NewPurchasePrice);
				return Ok(updatedItem);
			}
			catch (InvalidOperationException ex)
			{
				return NotFound(ex.Message);
			}
		}

		// --- Diğer İşlevsellik Uç Noktaları ---

		[HttpGet("{portfolioId}/total-value")]
		public async Task<IActionResult> GetTotalPortfolioValue(int portfolioId)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var portfolio = await _portfolioManager.GetPortfolioByIdAsync(portfolioId);
				if (portfolio == null || portfolio.UserId != userId)
				{
					return Forbid();
				}

				var value = await _portfolioManager.GetTotalPortfolioValueAsync(portfolioId, HttpContext);
				return Ok(value);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}

		}

		[HttpGet("{portfolioId}/profit-loss")]
		public async Task<IActionResult> GetTotalProfitLoss(int portfolioId)
		{
			try
			{
				var userId = await UserContextHelper.GetUserIdFromTokenCachedAsync(HttpContext, _cache);
				var portfolio = await _portfolioManager.GetPortfolioByIdAsync(portfolioId);
				if (portfolio == null || portfolio.UserId != userId)
				{
					return Forbid();
				}

				var profitLoss = await _portfolioManager.GetTotalProfitLossAsync(portfolioId, HttpContext);
				return Ok(profitLoss);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
