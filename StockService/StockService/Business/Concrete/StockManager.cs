using StockService.Business.Abstract;
using StockService.DataAccess.Abstract;
using StockService.DataAccess.Redis; // Redis servisi için
using StockService.Entities.Concrete;
using System.Text.Json; // JSON serileştirme için
using MassTransit;
using Shared.Contracts;

namespace StockService.Business.Concrete
{
	public class StockManager : IStockManager
	{
		private readonly IStockRepository _stockRepository;
		private readonly IExternalApiService _externalApiService;
        private readonly IRedisCacheService _redisCacheService; // Redis servisini enjekte et
        private readonly IPublishEndpoint _publishEndpoint;

		public StockManager(
			IStockRepository stockRepository,
            IExternalApiService externalApiService,
            IRedisCacheService redisCacheService,
            IPublishEndpoint publishEndpoint) // Constructor'a ekle
		{
			_stockRepository = stockRepository;
			_externalApiService = externalApiService;
            _redisCacheService = redisCacheService;
            _publishEndpoint = publishEndpoint;
		}

	public async Task<List<Stock>> GetAllStocksAsync()
	{
		// Veriyi öncelikle cache'ten almayı dene
		var cacheKey = "stocks:data:all";
		var cachedStocksJson = await _redisCacheService.GetValueAsync(cacheKey);

		if (!string.IsNullOrEmpty(cachedStocksJson))
		{
			try
			{
				// Cache'te varsa, doğrudan oradan dön
				// PropertyNameCaseInsensitive kullan (eski cache verileri PascalCase olabilir)
				var jsonOptions = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};
				var cachedStocks = JsonSerializer.Deserialize<List<Stock>>(cachedStocksJson, jsonOptions);
				if (cachedStocks != null && cachedStocks.Any())
				{
					Console.WriteLine($"Cache'ten {cachedStocks.Count} hisse çekildi.");
					return cachedStocks;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Cache'ten deserialize hatası: {ex.Message}");
				Console.WriteLine($"Cache temizleniyor ve veritabanından çekiliyor...");
				// Cache'teki bozuk veriyi temizle
				await _redisCacheService.Clear(cacheKey);
			}
		}

		// Cache'te yoksa veya hata varsa veritabanından al
		Console.WriteLine("Veritabanından hisse senetleri çekiliyor...");
		try
		{
			var stocks = await _stockRepository.GetAllAsync();
			Console.WriteLine($"Veritabanından {stocks?.Count ?? 0} hisse çekildi.");
			
			if (stocks == null)
			{
				Console.WriteLine("UYARI: GetAllAsync null döndü!");
				return new List<Stock>();
			}
			
			if (!stocks.Any())
			{
				Console.WriteLine("UYARI: Veritabanında hiç hisse senedi yok!");
				return new List<Stock>();
			}
			
			// İlk stock'u logla (debug için)
			var firstStock = stocks.First();
			Console.WriteLine($"İlk hisse örneği - Id: {firstStock.Id}, Symbol: {firstStock.Symbol}, CompanyName: {firstStock.CompanyName}, LastUpdate: {firstStock.LastUpdate}");

			if (stocks != null && stocks.Any())
			{
                // Veriyi cache'e kaydet (15 dakika geçerli olacak şekilde)
				try
				{
					var jsonOptions = new JsonSerializerOptions
					{
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
						WriteIndented = false
					};
                    await _redisCacheService.SetValueAsync(
						cacheKey,
						JsonSerializer.Serialize(stocks, jsonOptions),
                        TimeSpan.FromMinutes(15));
					Console.WriteLine("Stocks cache'e kaydedildi.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Cache'e kaydetme hatası (devam ediliyor): {ex.Message}");
				}
			}

			return stocks;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Veritabanından stock çekme hatası: {ex.Message}");
			Console.WriteLine($"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
			}
			return new List<Stock>();
		}
	}

		public async Task UpdateStocksFromExternalApiAsync()
		{
			var stocksFromApi = await _externalApiService.FetchBistStocksAsync();
			if (stocksFromApi == null || !stocksFromApi.Any())
			{
				return;
			}

            await _stockRepository.BulkUpsertAsync(stocksFromApi);

			var cacheKey = "stocks:data:all";
			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				WriteIndented = false
			};
            await _redisCacheService.SetValueAsync(
				cacheKey,
				JsonSerializer.Serialize(stocksFromApi, jsonOptions),
                TimeSpan.FromMinutes(15));

            // Event yayınla: her hisse için güncel fiyatı gönder
            foreach (var s in stocksFromApi)
            {
                await _publishEndpoint.Publish(new StockPriceUpdated(
                    s.Symbol,
                    s.CurrentPrice,
                    DateTime.UtcNow
                ));
            }
		}

	public async Task<Stock?> GetStockBySymbolAsync(string symbol)
	{
		return await _stockRepository.GetBySymbolAsync(symbol);
	}

	public async Task<List<StockHistoricalData>> GetHistoricalDataAsync(string symbol, string period, string interval)
	{
		Console.WriteLine($"[MANAGER] GetHistoricalDataAsync başladı - Symbol: {symbol}, Period: {period}, Interval: {interval}");
		
		// !!!! GEÇICI: Cache'i bypass et - debug için !!!!
		Console.WriteLine($"[MANAGER] ⚠️ CACHE BYPASS - Direkt Python'dan çekiliyor");
		
		var historicalData = await _externalApiService.FetchHistoricalDataAsync(symbol, period, interval);
		Console.WriteLine($"[MANAGER] Python'dan dönen veri sayısı: {historicalData?.Count ?? 0}");

		return historicalData;
	}
	}
}
