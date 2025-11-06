using Microsoft.SemanticKernel;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace LLMService.Plugins
{
	/// <summary>
	/// Hisse senedi bilgilerini çekmek için basit plugin
	/// </summary>
	public class StockPlugin
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public StockPlugin(
			IHttpClientFactory httpClientFactory,
			IConfiguration configuration,
			IHttpContextAccessor httpContextAccessor)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_httpContextAccessor = httpContextAccessor;
		}

		[KernelFunction]
		public async Task<string> GetStockPrice(
			string symbol)
		{
			try
			{
				var stockServiceUrl = _configuration["ServiceUrls:StockService"]
					?? "https://localhost:7208";
				var client = CreateClientWithAuth();
				var response = await client.GetAsync($"{stockServiceUrl}/api/stocks/{symbol}");

				if (!response.IsSuccessStatusCode)
				{
					return $"{symbol} hissesi için bilgi alınamadı. HTTP Status: {response.StatusCode}";
				}

				var jsonContent = await response.Content.ReadAsStringAsync();
				var stock = JsonSerializer.Deserialize<StockDto>(
					jsonContent,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (stock == null)
				{
					return $"{symbol} hissesi bulunamadı.";
				}

				return $@"{symbol} - {stock.CompanyName}
Güncel Fiyat: ₺{stock.CurrentPrice:F2}
Değişim: ₺{stock.Change:F2} ({stock.ChangePercent:F2}%)
Açılış: ₺{stock.OpenPrice:F2}
Yüksek: ₺{stock.HighPrice:F2}
Düşük: ₺{stock.LowPrice:F2}
Hacim: {stock.Volume:N0}
Son Güncelleme: {stock.LastUpdate:dd.MM.yyyy HH:mm}";
			}
			catch (Exception ex)
			{
				// Detaylı hata mesajı
				return $"{symbol} hissesi için bilgi alınırken hata oluştu: {ex.Message}. Detay: {ex.GetType().Name}";
			}
		}

		[KernelFunction]
		public async Task<string> GetCanonicalName(string symbol)
		{
			try
			{
				var stockServiceUrl = _configuration["ServiceUrls:StockService"]
					?? "https://localhost:7208";
				var client = CreateClientWithAuth();
				var response = await client.GetAsync($"{stockServiceUrl}/api/stocks/{symbol}");

				if (!response.IsSuccessStatusCode)
				{
					return $"{symbol} hissesi bulunamadı (HTTP {(int)response.StatusCode}).";
				}

				var jsonContent = await response.Content.ReadAsStringAsync();
				var stock = JsonSerializer.Deserialize<StockDto>(
					jsonContent,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				return stock?.CompanyName ?? $"{symbol} hissesi bulunamadı.";
			}
			catch (Exception ex)
			{
				return $"{symbol} için şirket adı alınamadı: {ex.Message}";
			}
		}

		[KernelFunction]
		public async Task<string> GetStockInfo(string symbol)
		{
			try
			{
				var stockServiceUrl = _configuration["ServiceUrls:StockService"]
					?? "https://localhost:7208";
				var client = CreateClientWithAuth();
				var response = await client.GetAsync($"{stockServiceUrl}/api/stocks/{symbol}");

				if (!response.IsSuccessStatusCode)
				{
					var error = new
					{
						ok = false,
						status = (int)response.StatusCode,
						symbol,
						message = "Hisse bilgisi alınamadı"
					};
					return JsonSerializer.Serialize(error);
				}

				var jsonContent = await response.Content.ReadAsStringAsync();
				var stock = JsonSerializer.Deserialize<StockDto>(
					jsonContent,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (stock == null)
				{
					var notFound = new { ok = false, symbol, message = "Hisse bulunamadı" };
					return JsonSerializer.Serialize(notFound);
				}

				var payload = new
				{
					ok = true,
					symbol = stock.Symbol,
					companyName = stock.CompanyName,
					currentPrice = stock.CurrentPrice,
					change = stock.Change,
					changePercent = stock.ChangePercent,
					openPrice = stock.OpenPrice,
					highPrice = stock.HighPrice,
					lowPrice = stock.LowPrice,
					volume = stock.Volume,
					lastUpdate = stock.LastUpdate,
					source = "StockService"
				};

				return JsonSerializer.Serialize(payload);
			}
			catch (Exception ex)
			{
				var error = new { ok = false, symbol, message = ex.Message };
				return JsonSerializer.Serialize(error);
			}
		}

		private System.Net.Http.HttpClient CreateClientWithAuth()
		{
			var client = _httpClientFactory.CreateClient();
			var httpContext = _httpContextAccessor.HttpContext;
			if (httpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) == true)
			{
				var token = authHeader.ToString();
				if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
				{
					client.DefaultRequestHeaders.Authorization =
						new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Substring(7));
				}
			}
			else
			{
				var accessToken = httpContext?.Request.Query["access_token"].ToString();
				if (!string.IsNullOrEmpty(accessToken))
				{
					client.DefaultRequestHeaders.Authorization =
						new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
				}
			}
			return client;
		}

		// Stock DTO (StockService'ten gelen response için)
		private class StockDto
		{
			public string Symbol { get; set; } = string.Empty;
			public string CompanyName { get; set; } = string.Empty;
			public decimal CurrentPrice { get; set; }
			public decimal Change { get; set; }
			public decimal ChangePercent { get; set; }
			public decimal OpenPrice { get; set; }
			public decimal HighPrice { get; set; }
			public decimal LowPrice { get; set; }
			public long Volume { get; set; }
			public DateTime LastUpdate { get; set; }
		}
	}
}
