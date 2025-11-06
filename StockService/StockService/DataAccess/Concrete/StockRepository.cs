using Microsoft.EntityFrameworkCore;
using StockService.DataAccess.Abstract;
using StockService.DataAccess.Context;
using StockService.Entities.Concrete;
using System.Linq.Expressions;

namespace StockService.DataAccess.Concrete
{
	public class StockRepository : GenericRepository<Stock>, IStockRepository
	{
		public StockRepository(StockDbContext context) : base(context)
		{
		}

		public async Task<Stock?> GetBySymbolAsync(string symbol)
		{
			// GenericRepository'den gelen GetAsync metodunu kullanıyoruz.
			return await GetAsync(s => s.Symbol == symbol);
		}

		public async Task BulkUpsertAsync(IEnumerable<Stock> stocks)
		{
			var symbols = stocks.Select(s => s.Symbol).ToList();
			var existingStocksDict = await _context.Stocks
				.Where(s => symbols.Contains(s.Symbol))
				.ToDictionaryAsync(s => s.Symbol);

			var stocksToUpdate = new List<Stock>();
			var stocksToAdd = new List<Stock>();

			foreach (var stock in stocks)
			{
				if (existingStocksDict.TryGetValue(stock.Symbol, out var existingStock))
				{
					existingStock.CurrentPrice = stock.CurrentPrice;
					existingStock.Change = stock.Change;
					existingStock.ChangePercent = stock.ChangePercent;
					existingStock.OpenPrice = stock.OpenPrice;
					existingStock.HighPrice = stock.HighPrice;
					existingStock.LowPrice = stock.LowPrice;
					existingStock.Volume = stock.Volume;
					existingStock.LastUpdate = DateTime.UtcNow; // PostgreSQL için UTC kullan
					stocksToUpdate.Add(existingStock);
				}
				else
				{
					stock.InsertDate = DateTime.UtcNow;
					stock.LastUpdate = DateTime.UtcNow;
					stocksToAdd.Add(stock);
				}
			}

			if (stocksToAdd.Any())
			{
				await _context.Stocks.AddRangeAsync(stocksToAdd);
			}
			if (stocksToUpdate.Any())
			{
				_context.Stocks.UpdateRange(stocksToUpdate);
			}

			await _context.SaveChangesAsync();
		}

	}
}
