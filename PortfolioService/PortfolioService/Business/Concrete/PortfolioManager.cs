using PortfolioService.Business.Abstract;
using PortfolioService.DataAccess.Abstract;
using PortfolioService.Entities.Concrete;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PortfolioService.Entities.Dtos;
using MassTransit; 
using Shared.Contracts; 
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt; 
using StackExchange.Redis; 
using StockService.DataAccess.Redis; 
using System.Net.Http.Headers; 
using System; 

namespace PortfolioService.Business.Concrete
{
	public class PortfolioManager : IPortfolioManager
	{
		private readonly IPortfolioRepository _portfolioRepository;
		private readonly HttpClient _httpClient;
		private readonly string _stockServiceBaseUrl;
		private readonly IRedisCacheService _redisCacheService;
		private readonly string _redisKeyPrefix = "stock_";
		private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(1);


		public PortfolioManager(
			IPortfolioRepository portfolioRepository,
			HttpClient httpClient,
			IConfiguration configuration,
			IRedisCacheService redisCacheService)
		{
			_portfolioRepository = portfolioRepository;
			_httpClient = httpClient;
			_stockServiceBaseUrl = configuration["StockService:BaseUrl"] ?? throw new InvalidOperationException("StockService:BaseUrl configuration is missing!");

			// Debug: Configuration değerini logla
			Console.WriteLine($"[PortfolioManager] StockService BaseUrl: {_stockServiceBaseUrl}");
			_redisCacheService = redisCacheService;
		}

		// --- Portföy İşlemleri ---
		public async Task<List<Portfolio>> GetAllPortfoliosAsync()
		{
			return await _portfolioRepository.GetAllPortfoliosAsync();
		}

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
	public async Task<PortfolioItem> AddItemToPortfolioAsync(int portfolioId, string symbol, decimal purchasePrice, int quantity, HttpContext? httpContext = null)
	{
		// GetStockFromStockServiceAsync artık Redis'i kontrol ediyor.
		var stock = await GetStockFromStockServiceAsync(symbol, httpContext);
		if (stock == null)
		{
			throw new InvalidOperationException($"'{symbol}' sembolü sistemde bulunamadı. Lütfen geçerli bir hisse senedi sembolü girin.");
		}

		var item = new PortfolioItem
		{
			PortfolioId = portfolioId,
			Symbol = symbol,
			PurchaseDate = DateTime.UtcNow,
			AverageCost = purchasePrice,
			Quantity = quantity,
			CurrentPrice = stock.CurrentPrice
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
	public async Task<TotalValueDto> GetTotalPortfolioValueAsync(int portfolioId, HttpContext? httpContext = null)
	{
		var portfolio = await _portfolioRepository.GetPortfolioByIdAsync(portfolioId);
		if (portfolio == null || portfolio.PortfolioItems == null)
		{
			return null;
		}

		TotalValueDto totalValue = new();
		foreach (var item in portfolio.PortfolioItems)
		{
			// Redis'i kullanan metodumuz çağrılıyor
			var stock = await GetStockFromStockServiceAsync(item.Symbol, httpContext);
			if (stock != null)
			{
				totalValue.TotalValue += stock.CurrentPrice * item.Quantity;
			}
			// Maliyet hesapla (current price olmasa bile)
			totalValue.TotalCost += item.AverageCost * item.Quantity;
		}
		return totalValue;
	}

	public async Task<ProfitLossDto> GetTotalProfitLossAsync(int portfolioId, HttpContext? httpContext = null)
	{
		var portfolio = await _portfolioRepository.GetPortfolioByIdAsync(portfolioId);
		if (portfolio == null || portfolio.PortfolioItems == null)
		{
			return null;
		}
		
		decimal totalCurrentValue = 0;
		decimal totalInvestedValue = 0;
		
		foreach (var item in portfolio.PortfolioItems)
		{
			// Redis'i kullanan metodumuz çağrılıyor
			var stock = await GetStockFromStockServiceAsync(item.Symbol, httpContext);
			if (stock != null)
			{
				totalCurrentValue += stock.CurrentPrice * item.Quantity;
			}
			totalInvestedValue += item.AverageCost * item.Quantity;
		}
		
		var profitLoss = totalCurrentValue - totalInvestedValue;
		var profitLossPercentage = totalInvestedValue > 0 ? (profitLoss / totalInvestedValue) * 100 : 0;
		
		return new ProfitLossDto
		{
			TotalProfitLoss = profitLoss,
			TotalProfitLossPercentage = profitLossPercentage
		};
	}

		// --- Redis Cache-Aside ve HTTP İstek Metodu ---
		private async Task<StockDto> GetStockFromStockServiceAsync(string symbol, HttpContext? httpContext = null)
		{
			var redisKey = $"{_redisKeyPrefix}{symbol}";

			// -----------------------------------------------------------------
			// 1. ÖNCE REDIS'İ KONTROL ET (CACHE READ)
			// -----------------------------------------------------------------
			try
			{
				// Güncellenen arayüz metodu GetStringAsync çağrılıyor
				var cachedJson = await _redisCacheService.GetStringAsync(redisKey);
				if (!string.IsNullOrEmpty(cachedJson))
				{
					// Cache Hit: Redis'te bulundu, anında döndür
					var cachedStockDto = JsonSerializer.Deserialize<StockDto>(cachedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Console.WriteLine($"[PortfolioManager] Cache Hit: Retrieved stock {symbol} from Redis.");
					return cachedStockDto;
				}
				Console.WriteLine($"[PortfolioManager] Cache Miss: Stock {symbol} not found in Redis. Proceeding with HTTP request.");
			}
			catch (Exception ex)
			{
				// Redis bağlantı hatası durumunda bile HTTP isteğine geç.
				Console.WriteLine($"[PortfolioManager] Redis read error: {ex.Message}. Proceeding with HTTP request.");
			}

			// -----------------------------------------------------------------
			// 2. HTTP İSTEĞİ (CACHE MISS veya REDIS HATASI VARSA)
			// -----------------------------------------------------------------

			var requestUrl = $"{_stockServiceBaseUrl}/api/stocks/{symbol}";

			try
			{
				Console.WriteLine($"[PortfolioManager] Making request to: {requestUrl}");

				using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

				// Authorization header'ı ekle (HttpContext'ten)
				if (httpContext != null && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
				{
					request.Headers.Add("Authorization", authHeader.ToString());
					Console.WriteLine($"[PortfolioManager] Added Authorization header from HttpContext");
				}

				using var response = await _httpClient.SendAsync(request);

				Console.WriteLine($"[PortfolioManager] Response Status: {response.StatusCode}");

				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					Console.WriteLine($"[PortfolioManager] Stock {symbol} not found in StockService");
					return null;
				}

				if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					Console.WriteLine($"[PortfolioManager] Unauthorized - JWT token may be invalid or expired");
					throw new UnauthorizedAccessException("StockService authentication failed. Please check JWT token.");
				}

				response.EnsureSuccessStatusCode();
				var json = await response.Content.ReadAsStringAsync();
				var stockDto = JsonSerializer.Deserialize<StockDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				// -----------------------------------------------------------------
				// 3. CACHE'E YAZMA (Başarılı HTTP isteği sonrası)
				// -----------------------------------------------------------------
				try
				{
					// Başarılı sonucu Redis'e yaz (GetStringAsync ile uyumlu)
					await _redisCacheService.SetStringAsync(redisKey, json, _cacheExpiration);
					Console.WriteLine($"[PortfolioManager] Wrote stock {symbol} to Redis cache with expiration {_cacheExpiration.TotalSeconds} seconds.");
				}
				catch (Exception ex)
				{
					// Cache'e yazma hatası, ana iş akışını durdurmamalı.
					Console.WriteLine($"[PortfolioManager] Redis write failed: {ex.Message}");
				}

				Console.WriteLine($"[PortfolioManager] Successfully retrieved stock: {symbol}");
				return stockDto;
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"[PortfolioManager] HTTP Request failed: {ex.Message}");
				throw new InvalidOperationException($"Failed to connect to the Stock Service. URL: {requestUrl}", ex);
			}
			catch (TaskCanceledException ex)
			{
				Console.WriteLine($"[PortfolioManager] Request timeout: {ex.Message}");
				throw new InvalidOperationException($"Request to Stock Service timed out. URL: {requestUrl}", ex);
			}
			catch (UnauthorizedAccessException)
			{
				throw; // Re-throw unauthorized exceptions
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[PortfolioManager] Unexpected error: {ex.Message}");
				throw new InvalidOperationException($"Unexpected error when connecting to Stock Service: {ex.Message}", ex);
			}
		}
	}
}
