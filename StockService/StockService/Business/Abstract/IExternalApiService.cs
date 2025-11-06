using StockService.Entities.Concrete;

namespace StockService.Business.Abstract
{
	public interface IExternalApiService
	{
		Task<List<Stock>> FetchBistStocksAsync();
		Task<List<StockHistoricalData>> FetchHistoricalDataAsync(string symbol, string period, string interval);
	}
}
