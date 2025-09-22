using PortfolioService.Business.Abstract;
using PortfolioService.DataAccess.Abstract;
using PortfolioService.Entities.Concrete;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PortfolioService.Entities.Dtos;

namespace PortfolioService.Business.Concrete
{
	public class PortfolioManager : IPortfolioManager
	{
		private readonly IPortfolioRepository _portfolioRepository;
		private readonly HttpClient _httpClient;
		private readonly string _stockServiceBaseUrl;

		public PortfolioManager(IPortfolioRepository portfolioRepository, HttpClient httpClient, IConfiguration configuration)
		{
			_portfolioRepository = portfolioRepository;
			_httpClient = httpClient;
			_stockServiceBaseUrl = configuration["StockService:BaseUrl"];
		}

		// --- Portföy İşlemleri ---
		public async Task<List<Portfolio>> GetAllPortfoliosByUserIdAsync(int userId)
		{
			return await _portfolioRepository.GetAllPortfoliosByUserIdAsync(userId);
		}

		public async Task<Portfolio> GetPortfolioByIdAsync(int portfolioId)
		{
			return await _portfolioRepository.GetPortfolioByIdAsync(portfolioId);
		}

		public async Task<Portfolio> CreatePortfolioAsync(int userId, string name)
		{
			var portfolio = new Portfolio
			{
				UserId = userId,
				Name = name,
				CreationDate = DateTime.UtcNow
			};
			await _portfolioRepository.AddPortfolioAsync(portfolio);
			return portfolio;
		}

		public async Task UpdatePortfolioNameAsync(int portfolioId, string newName)
		{
			var portfolio = await _portfolioRepository.GetPortfolioByIdAsync(portfolioId);
			if (portfolio == null)
			{
				throw new InvalidOperationException("Portfolio not found.");
			}
			portfolio.Name = newName;
			await _portfolioRepository.UpdatePortfolioAsync(portfolio);
		}

		public async Task DeletePortfolioAsync(int portfolioId)
		{
			await _portfolioRepository.DeletePortfolioAsync(portfolioId);
		}

		// --- Portföy Varlık (Item) İşlemleri ---
		public async Task<PortfolioItem> AddItemToPortfolioAsync(int portfolioId, string symbol, decimal purchasePrice, int quantity)
		{
			var stock = await GetStockFromStockServiceAsync(symbol);
			if (stock == null)
			{
				throw new InvalidOperationException($"Stock with symbol '{symbol}' not found in the Stock Service.");
			}

			var item = new PortfolioItem
			{
				PortfolioId = portfolioId,
				Symbol = symbol,
				PurchaseDate = DateTime.UtcNow,
				AverageCost = purchasePrice,
				Quantity = quantity
			};

			await _portfolioRepository.AddPortfolioItemAsync(item);
			return item;
		}

		public async Task<PortfolioItem> UpdatePortfolioItemAsync(int portfolioItemId, int newQuantity, decimal newPurchasePrice)
		{
			var item = await _portfolioRepository.GetPortfolioItemByIdAsync(portfolioItemId);
			if (item == null)
			{
				throw new InvalidOperationException("Portfolio item not found.");
			}
			item.Quantity = newQuantity;
			item.AverageCost = newPurchasePrice;
			await _portfolioRepository.UpdatePortfolioItemAsync(item);
			return item;
		}

		public async Task DeletePortfolioItemAsync(int portfolioItemId)
		{
			await _portfolioRepository.DeletePortfolioItemAsync(portfolioItemId);
		}

		// --- İş Mantığı ve Servisler Arası İletişim ---
		public async Task<decimal> GetTotalPortfolioValueAsync(int portfolioId)
		{
			var portfolio = await _portfolioRepository.GetPortfolioByIdAsync(portfolioId);
			if (portfolio == null || !portfolio.PortfolioItems.Any())
			{
				return 0;
			}

			decimal totalValue = 0;
			foreach (var item in portfolio.PortfolioItems)
			{
				var stock = await GetStockFromStockServiceAsync(item.Symbol);
				if (stock != null)
				{
					totalValue += stock.CurrentPrice * item.Quantity;
				}
			}
			return totalValue;
		}

		public async Task<decimal> GetTotalProfitLossAsync(int portfolioId)
		{
			var portfolio = await _portfolioRepository.GetPortfolioByIdAsync(portfolioId);
			if (portfolio == null || !portfolio.PortfolioItems.Any())
			{
				return 0;
			}
			decimal totalProfitLoss = 0;
			foreach(var item in portfolio.PortfolioItems)
			{
				var stock = await GetStockFromStockServiceAsync(item.Symbol);
				if (stock != null)
				{
					var currentValue = stock.CurrentPrice * item.Quantity;
					var investedValue = item.AverageCost * item.Quantity;
					totalProfitLoss += (currentValue - investedValue);
				}
			}
			return totalProfitLoss;
		}

		private async Task<StockDto> GetStockFromStockServiceAsync(string symbol)
		{
			try
			{
				using var response = await _httpClient.GetAsync($"{_stockServiceBaseUrl}/api/stocks/{symbol}");
				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					return null;
				}
				response.EnsureSuccessStatusCode();
				var json = await response.Content.ReadAsStringAsync();
				var stockDto = JsonSerializer.Deserialize<StockDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				return stockDto;
			}
			catch (HttpRequestException ex)
			{
				throw new InvalidOperationException("Failed to connect to the Stock Service.", ex);
			}
		}
	}

	
}