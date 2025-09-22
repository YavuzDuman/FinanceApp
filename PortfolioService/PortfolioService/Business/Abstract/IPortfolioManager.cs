using PortfolioService.Entities.Concrete;

namespace PortfolioService.Business.Abstract
{
	public interface IPortfolioManager
	{
		Task<List<Portfolio>> GetAllPortfoliosByUserIdAsync(int userId);
		Task<Portfolio> GetPortfolioByIdAsync(int portfolioId);
		Task<Portfolio> CreatePortfolioAsync(int userId, string name);
		Task UpdatePortfolioNameAsync(int portfolioId, string newName);
		Task DeletePortfolioAsync(int portfolioId);

		Task<PortfolioItem> AddItemToPortfolioAsync(int portfolioId, string symbol, decimal purchasePrice, int quantity);
		Task<PortfolioItem> UpdatePortfolioItemAsync(int portfolioItemId, int newQuantity, decimal newPurchasePrice);
		Task DeletePortfolioItemAsync(int portfolioItemId);

		Task<decimal> GetTotalPortfolioValueAsync(int portfolioId);
		Task<decimal> GetTotalProfitLossAsync(int portfolioId);
	}
}
