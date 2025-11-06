using Microsoft.EntityFrameworkCore;
using WatchlistService.DataAccess.Abstract;
using WatchlistService.DataAccess.Context;
using WatchlistService.Entities;

namespace WatchlistService.DataAccess.Concrete
{
	public class WatchlistRepository : IWatchlistRepository
	{
		private readonly WatchlistDbContext _context;

		public WatchlistRepository(WatchlistDbContext context)
		{
			_context = context;
		}

		public Task<Watchlist?> GetListByUserIdAsync(int userId)
		{
			return _context.Watchlists
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.FirstOrDefaultAsync(w => w.UserId == userId);
		}

		public async Task<Watchlist?> GetWatchlistWithItemsAsync(int id)
		{
			return await _context.Watchlists
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.Include(w => w.Items)
				.FirstOrDefaultAsync(w => w.Id == id);
		}

		public async Task<List<Watchlist>> GetAllByUserIdAsync(int userId)
		{
			return await _context.Watchlists
				.AsNoTracking() // ⚡ Change tracking'i kapat - read-only sorgu
				.Include(w => w.Items)
				.Where(w => w.UserId == userId)
				.ToListAsync();
		}

		public async Task<Watchlist> AddWatchlistAsync(Watchlist list)
		{
			_context.Watchlists.Add(list);
			await _context.SaveChangesAsync();
			return list;
		}

		public async Task<Watchlist> UpdateWatchlistAsync(Watchlist list)
		{
			_context.Watchlists.Update(list);
			await _context.SaveChangesAsync();
			return list;
		}

		public async Task DeleteWatchlistAsync(int id)
		{
			var listToDelete = await _context.Watchlists.FindAsync(id);
			if (listToDelete != null)
			{
				_context.Watchlists.Remove(listToDelete);
				await _context.SaveChangesAsync();
			}
		}



		public async Task<WatchlistItem> AddItemToWatchlistAsync(WatchlistItem item)
		{
			_context.WatchlistItems.Add(item);
			await _context.SaveChangesAsync();
			return item;
		}

		public async Task<WatchlistItem?> GetItemByIdAsync(int itemId)
		{
			return await _context.WatchlistItems.FindAsync(itemId).AsTask();
		}

		public async Task<WatchlistItem> UpdateWatchlistItemAsync(WatchlistItem item)
		{
			_context.WatchlistItems.Update(item);
			await _context.SaveChangesAsync();
			return item;
		}

		public async Task RemoveItemFromWatchlistAsync(int itemId)
		{
			var itemToDelete = await _context.WatchlistItems.FindAsync(itemId);
			if (itemToDelete != null)
			{
				_context.WatchlistItems.Remove(itemToDelete);
				await _context.SaveChangesAsync();
			}
		}

		public async Task UpdateCurrentPriceBySymbolAsync(string symbol, decimal currentPrice)
		{
			var items = await _context.WatchlistItems
				.Where(pi => pi.Symbol == symbol)
				.ToListAsync();
			if (items.Count == 0) return;
			foreach (var item in items)
			{
				item.CurrentPrice = currentPrice;
			}
			_context.WatchlistItems.UpdateRange(items);
			await _context.SaveChangesAsync();
		}

	}
}
