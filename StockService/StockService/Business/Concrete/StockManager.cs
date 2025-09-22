using StockService.Business.Abstract;
using StockService.DataAccess.Abstract;
using StockService.DataAccess.Redis; // Redis servisi için
using StockService.Entities.Concrete;
using System.Text.Json; // JSON serileştirme için

namespace StockService.Business.Concrete
{
	public class StockManager : IStockManager
	{
		private readonly IStockRepository _stockRepository;
		private readonly IExternalApiService _externalApiService;
		private readonly IRedisCacheService _redisCacheService; // Redis servisini enjekte et

		public StockManager(
			IStockRepository stockRepository,
			IExternalApiService externalApiService,
			IRedisCacheService redisCacheService) // Constructor'a ekle
		{
			_stockRepository = stockRepository;
			_externalApiService = externalApiService;
			_redisCacheService = redisCacheService;
		}

		public async Task<List<Stock>> GetAllStocksAsync()
		{
			// Veriyi öncelikle cache'ten almayı dene
			var cacheKey = "stocks:data:all";
			var cachedStocksJson = await _redisCacheService.GetValueAsync(cacheKey);

			if (!string.IsNullOrEmpty(cachedStocksJson))
			{
				// Cache'te varsa, doğrudan oradan dön
				return JsonSerializer.Deserialize<List<Stock>>(cachedStocksJson);
			}

			// Cache'te yoksa veritabanından al
			var stocks = await _stockRepository.GetAllAsync();

			// Veriyi cache'e kaydet (10 dakika geçerli olacak şekilde)
			await _redisCacheService.SetValueAsync(
				cacheKey,
				JsonSerializer.Serialize(stocks),
				TimeSpan.FromMinutes(10));

			return stocks;
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
			await _redisCacheService.SetValueAsync(
				cacheKey,
				JsonSerializer.Serialize(stocksFromApi),
				TimeSpan.FromMinutes(10));
		}

		public async Task<Stock?> GetStockBySymbolAsync(string symbol)
		{
			// Bu metot IStockManager'da tanımlı olduğu için eklenmiştir.
			// Eğer tekil hisse senedi verisini de cache'te tutmak istersen buraya cacheleme mantığı ekleyebilirsin.
			return await _stockRepository.GetBySymbolAsync(symbol);
		}
	}
}
