using System.Net.Http;
using System.Text.Json;
using WatchlistService.Business.Abstract;
using WatchlistService.DataAccess.Abstract;
using WatchlistService.Entities;
using WatchlistService.Entities.Dtos;
using StockService.DataAccess.Redis;

namespace WatchlistService.Business.Concrete
{
	public class WatchlistManager : IWatchlistManager
	{
		private readonly IWatchlistRepository _watchlistRepository;
		private readonly HttpClient _httpClient;
		private readonly string _stockServiceBaseUrl;
		private readonly IRedisCacheService _redisCacheService;
		private readonly string _redisKeyPrefix = "stock_";
		private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(1);
		public WatchlistManager(
			IWatchlistRepository watchlistRepository,
			HttpClient httpClient,
			IConfiguration configuration,
			IRedisCacheService redisCacheService)
		{
			_watchlistRepository = watchlistRepository;
			_httpClient = httpClient;
			_stockServiceBaseUrl = configuration["StockService:BaseUrl"] ?? throw new InvalidOperationException("StockService:BaseUrl configuration is missing!");

			// Debug: Configuration değerini logla
			Console.WriteLine($"[PortfolioManager] StockService BaseUrl: {_stockServiceBaseUrl}");
			_redisCacheService = redisCacheService;
		}

		public async Task<Watchlist?> GetWatchlistWithItemsAsync(int id)
		{
			return await _watchlistRepository.GetWatchlistWithItemsAsync(id);
		}
		public async Task<List<Watchlist>> GetAllByUserIdAsync(int userId)
		{
			return await _watchlistRepository.GetAllByUserIdAsync(userId);
		}
		public async Task<Watchlist> AddWatchlistAsync(int userId, string name, string? description)
		{
			var list = new Watchlist
			{
				UserId = userId,
				ListName = name,
				CreatedAt = DateTime.UtcNow,
				Description = description
			};
			return await _watchlistRepository.AddWatchlistAsync(list);
		}

		public async Task<Watchlist> UpdateWatchlistAsync(int watchlistId, int userId, string? newName, string? description)
		{
			var list = await _watchlistRepository.GetWatchlistWithItemsAsync(watchlistId);

			if (list == null)
			{
				throw new Exception("Watchlist not found");
			}

			if (list.UserId != userId)
			{
				throw new UnauthorizedAccessException("Bu watchlist'e erişim yetkiniz yok");
			}

			if (!string.IsNullOrWhiteSpace(newName))
			{
				list.ListName = newName;
			}

			if (description != null)
			{
				list.Description = description;
			}

			return await _watchlistRepository.UpdateWatchlistAsync(list);
		}

		public async Task DeleteWatchlistAsync(int id, int userId)
		{
			var list = await _watchlistRepository.GetWatchlistWithItemsAsync(id);
			if (list == null)
			{
				throw new Exception("Watchlist not found");
			}

			if (list.UserId != userId)
			{
				throw new UnauthorizedAccessException("Bu watchlist'e erişim yetkiniz yok");
			}

			await _watchlistRepository.DeleteWatchlistAsync(id);
		}



		public async Task<WatchlistItem> AddItemToWatchlistAsync(int watchlistId, string symbol, int userId, HttpContext? httpContext = null,string? note= null)
		{
			var watchlist = await _watchlistRepository.GetWatchlistWithItemsAsync(watchlistId);
			if (watchlist == null)
			{
				throw new Exception("Watchlist not found");
			}

			if (watchlist.UserId != userId)
			{
				throw new UnauthorizedAccessException("Bu watchlist'e erişim yetkiniz yok");
			}

			var stock = await GetStockFromStockServiceAsync(symbol, httpContext);
			if (stock == null)
			{
				throw new Exception("Stock not found in StockService");
			}

			// Aynı symbol'ün zaten listede olup olmadığını kontrol et
			if (watchlist.Items.Any(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidOperationException($"Bu hisse ({symbol}) zaten watchlist'te mevcut");
			}

			var item = new WatchlistItem
			{
				WatchlistId = watchlistId,
				Symbol = stock.Symbol,
				EntryDate = DateTime.UtcNow,
				CurrentPrice = stock.CurrentPrice,
				Note = note
			};
			await _watchlistRepository.AddItemToWatchlistAsync(item);
			return item;
		}
		public async Task<WatchlistItem?> GetItemByIdAsync(int itemId)
		{
			return await _watchlistRepository.GetItemByIdAsync(itemId);
		}

		public async Task<WatchlistItem> UpdateWatchlistItemAsync(int itemId, string? note, int userId)
		{
			var item = await _watchlistRepository.GetItemByIdAsync(itemId);
			if (item == null)
			{
				throw new Exception("Watchlist item not found");
			}

			var watchlist = await _watchlistRepository.GetWatchlistWithItemsAsync(item.WatchlistId);
			if (watchlist == null || watchlist.UserId != userId)
			{
				throw new UnauthorizedAccessException("Bu item'e erişim yetkiniz yok");
			}

			if (!string.IsNullOrWhiteSpace(note))
			{
				item.Note = note;
			}

			return await _watchlistRepository.UpdateWatchlistItemAsync(item);
		}
		
		public async Task RemoveItemFromWatchlistAsync(int itemId, int userId)
		{
			var item = await _watchlistRepository.GetItemByIdAsync(itemId);
			if (item == null)
			{
				throw new Exception("Watchlist item not found");
			}

			var watchlist = await _watchlistRepository.GetWatchlistWithItemsAsync(item.WatchlistId);
			if (watchlist == null || watchlist.UserId != userId)
			{
				throw new UnauthorizedAccessException("Bu item'e erişim yetkiniz yok");
			}

			await _watchlistRepository.RemoveItemFromWatchlistAsync(itemId);
		}


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
