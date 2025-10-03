using PortfolioService.Entities.Concrete;
using PortfolioService.Entities.Dtos;
using Microsoft.AspNetCore.Http;

namespace PortfolioService.Business.Abstract
{
	public interface IPortfolioManager
	{
		Task<List<Portfolio>> GetAllPortfoliosAsync();
		Task<List<Portfolio>> GetAllPortfoliosByUserIdAsync(int userId);
		Task<Portfolio> GetPortfolioByIdAsync(int portfolioId);
		Task<Portfolio> CreatePortfolioAsync(int userId, string name);
		Task UpdatePortfolioNameAsync(int portfolioId, string newName);
		Task DeletePortfolioAsync(int portfolioId);

		Task<PortfolioItem> AddItemToPortfolioAsync(int portfolioId, string symbol, decimal purchasePrice, int quantity, HttpContext? httpContext = null);
		Task<PortfolioItem> UpdatePortfolioItemAsync(int portfolioItemId, int newQuantity, decimal newPurchasePrice);
		Task DeletePortfolioItemAsync(int portfolioItemId);

		Task<TotalValueDto> GetTotalPortfolioValueAsync(int portfolioId, HttpContext? httpContext = null);
		Task<ProfitLossDto> GetTotalProfitLossAsync(int portfolioId, HttpContext? httpContext = null);
	}
}
