using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortfolioService.Business.Abstract;
using PortfolioService.Entities.Dtos;
using System.Security.Claims;

namespace PortfolioService.Controllers
{
	
	[ApiController]
	[Route("api/portfolios")]
	[Authorize]
	public class PortfolioController : ControllerBase
	{
		private readonly IPortfolioManager _portfolioManager;

		public PortfolioController(IPortfolioManager portfolioManager)
		{
			_portfolioManager = portfolioManager;
		}

		private int GetUserIdFromToken()
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
							  User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

			if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
			{
				throw new InvalidOperationException("User ID not found in token.");
			}
			return userId;
		}

		[HttpPost]
		public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioDto createDto)
		{
			var userId = GetUserIdFromToken();
			var portfolio = await _portfolioManager.CreatePortfolioAsync(userId, createDto.Name);
			return CreatedAtAction(nameof(GetPortfolioById), new { id = portfolio.Id }, portfolio);
		}

		[HttpGet]
		public async Task<IActionResult> GetAllPortfolios()
		{
			var userId = GetUserIdFromToken();
			var portfolios = await _portfolioManager.GetAllPortfoliosByUserIdAsync(userId);
			return Ok(portfolios);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetPortfolioById(int id)
		{
			var portfolio = await _portfolioManager.GetPortfolioByIdAsync(id);
			if (portfolio == null)
			{
				return NotFound();
			}
			// Güvenlik: Portföyün, istek atan kullanıcıya ait olduğunu doğrula
			// var userId = GetUserIdFromToken();
			// if (portfolio.UserId != userId) return Forbid();
			return Ok(portfolio);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdatePortfolioName(int id, [FromBody] UpdatePortfolioNameDto updateDto)
		{
			try
			{
				await _portfolioManager.UpdatePortfolioNameAsync(id, updateDto.NewName);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				return NotFound(ex.Message);
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeletePortfolio(int id)
		{
			await _portfolioManager.DeletePortfolioAsync(id);
			return NoContent();
		}

		// --- Portföy Varlık (Item) Uç Noktaları ---

		[HttpPost("{portfolioId}/items")]
		public async Task<IActionResult> AddItemToPortfolio(int portfolioId, [FromBody] AddItemToPortfolioDto itemDto)
		{
			try
			{
				var addedItem = await _portfolioManager.AddItemToPortfolioAsync(
					portfolioId,
					itemDto.Symbol,
					itemDto.PurchasePrice,
					itemDto.Quantity
				);
				return Ok(addedItem);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		// --- Diğer İşlevsellik Uç Noktaları ---

		[HttpGet("{portfolioId}/total-value")]
		public async Task<IActionResult> GetTotalPortfolioValue(int portfolioId)
		{
			var value = await _portfolioManager.GetTotalPortfolioValueAsync(portfolioId);
			return Ok(new { TotalValue = value });
		}

		[HttpGet("{portfolioId}/profit-loss")]
		public async Task<IActionResult> GetTotalProfitLoss(int portfolioId)
		{
			var profitLoss = await _portfolioManager.GetTotalProfitLossAsync(portfolioId);
			return Ok(new { ProfitLoss = profitLoss });
		}
	}
}
