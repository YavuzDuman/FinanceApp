using StockService.Entities.Concrete;

namespace StockService.Business.Abstract
{
	public interface IStockManager
	{
		Task<List<Stock>> GetAllStocksAsync();
		Task UpdateStocksFromExternalApiAsync();
		Task<Stock?> GetStockBySymbolAsync(string symbol);
		Task<List<StockHistoricalData>> GetHistoricalDataAsync(string symbol, string period, string interval);
	}
}
