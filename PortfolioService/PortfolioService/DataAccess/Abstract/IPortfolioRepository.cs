using PortfolioService.Entities.Concrete;

namespace PortfolioService.DataAccess.Abstract
{
	public interface IPortfolioRepository
	{
		Task<List<Portfolio>> GetAllPortfoliosAsync();
		Task<Portfolio> GetPortfolioByIdAsync(int portfolioId);
		Task<List<Portfolio>> GetAllPortfoliosByUserIdAsync(int userId);
		Task AddPortfolioAsync(Portfolio portfolio);
		Task UpdatePortfolioAsync(Portfolio portfolio);
		Task DeletePortfolioAsync(int portfolioId);

		Task<PortfolioItem> GetPortfolioItemByIdAsync(int portfolioItemId);
		Task<List<PortfolioItem>> GetPortfolioItemsByPortfolioIdAsync(int portfolioId);
		Task AddPortfolioItemAsync(PortfolioItem item);
		Task UpdatePortfolioItemAsync(PortfolioItem item);
		Task DeletePortfolioItemAsync(int portfolioItemId);
		Task UpdateCurrentPriceBySymbolAsync(string symbol, decimal currentPrice);
	}
}
