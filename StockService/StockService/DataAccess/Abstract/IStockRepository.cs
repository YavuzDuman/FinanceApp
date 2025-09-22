using StockService.Entities.Concrete;

namespace StockService.DataAccess.Abstract
{
	public interface IStockRepository : IGenericRepository<Stock>
	{
		Task<Stock?> GetBySymbolAsync(string symbol);
		Task BulkUpsertAsync(IEnumerable<Stock> stocks);
	}
}