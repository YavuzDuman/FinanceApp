using Microsoft.SemanticKernel;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace LLMService.Plugins
{
	/// <summary>
	/// Finansal tablolarý çekmek için plugin
	/// </summary>
	public class FinancialStatementPlugin
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public FinancialStatementPlugin(
			IHttpClientFactory httpClientFactory,
			IConfiguration configuration,
			IHttpContextAccessor httpContextAccessor)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_httpContextAccessor = httpContextAccessor;
		}

		[KernelFunction]
		public async Task<string> GetFinancialStatements(string symbol)
		{
			try
			{
				// FinancialStatementService URL'i (API Gateway üzerinden)
				var financialStatementServiceUrl = _configuration["ServiceUrls:FinancialStatementService"]
					?? "https://localhost:5000"; // API Gateway URL'i

				var client = _httpClientFactory.CreateClient();

				// Authorization header'ýný HttpContext'ten al
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
					// Query string'den token almayý dene (SignalR için)
					var accessToken = httpContext?.Request.Query["access_token"].ToString();
					if (!string.IsNullOrEmpty(accessToken))
					{
						client.DefaultRequestHeaders.Authorization =
							new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
					}
				}

				// API Gateway üzerinden finansal tablolarý çek
				var response = await client.GetAsync($"{financialStatementServiceUrl}/api/financialstatement/symbol/{symbol}");

				if (!response.IsSuccessStatusCode)
				{
					if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
						return $"{symbol} hissesi için finansal tablo bulunamadý.";
					}
					return $"{symbol} hissesi için finansal tablo bilgisi alýnamadý. HTTP Status: {response.StatusCode}";
				}

				var jsonContent = await response.Content.ReadAsStringAsync();
				var statements = JsonSerializer.Deserialize<List<FinancialStatementDto>>(
					jsonContent,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (statements == null || statements.Count == 0)
				{
					return $"{symbol} hissesi için finansal tablo bulunamadý.";
				}

				// En son açýklanan bilançoyu bul
				var latestStatement = statements
					.OrderByDescending(s => s.AnnouncementDate ?? s.StatementDate)
					.FirstOrDefault();

				if (latestStatement == null)
				{
					return $"{symbol} hissesi için finansal tablo bulunamadý.";
				}

				var result = $@"{symbol} - {latestStatement.CompanyName}
Bilanço Türü: {latestStatement.Type}
Bilanço Tarihi: {latestStatement.StatementDate:dd.MM.yyyy}
Açýklama Tarihi: {(latestStatement.AnnouncementDate.HasValue ? latestStatement.AnnouncementDate.Value.ToString("dd.MM.yyyy") : "Belirtilmemiþ")}";

				if (latestStatement.NetProfitChangeRate.HasValue)
				{
					result += $@"
Net Kar Deðiþim Oraný: {latestStatement.NetProfitChangeRate.Value:F2}%";
				}

				// Eðer birden fazla bilanço varsa, diðerlerini de özetle
				if (statements.Count > 1)
				{
					result += $@"

Toplam {statements.Count} adet finansal tablo mevcut. En son açýklanan bilanço yukarýda gösterilmektedir.";
				}

				return result;
			}
			catch (Exception ex)
			{
				return $"{symbol} hissesi için finansal tablo bilgisi alýnýrken hata oluþtu: {ex.Message}";
			}
		}

		// FinancialStatement DTO
		private class FinancialStatementDto
		{
			public int Id { get; set; }
			public string StockSymbol { get; set; } = string.Empty;
			public string CompanyName { get; set; } = string.Empty;
			public DateTime StatementDate { get; set; }
			public string Type { get; set; } = string.Empty;
			public string Data { get; set; } = string.Empty;
			public DateTime? AnnouncementDate { get; set; }
			public decimal? NetProfitChangeRate { get; set; }
			public DateTime UpdatedDate { get; set; }
		}
	}
}

