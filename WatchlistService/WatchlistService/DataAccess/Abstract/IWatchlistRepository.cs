using System.Collections.Generic;
using WatchlistService.Entities;

namespace WatchlistService.DataAccess.Abstract
{
	public interface IWatchlistRepository
	{
		Task<Watchlist?> GetWatchlistWithItemsAsync(int id);
		Task<List<Watchlist>> GetAllByUserIdAsync(int userId);
		Task<Watchlist?> GetListByUserIdAsync(int userId);
		Task<Watchlist> AddWatchlistAsync(Watchlist list);
		Task<Watchlist> UpdateWatchlistAsync(Watchlist list);
		Task DeleteWatchlistAsync(int id);

		Task<WatchlistItem> AddItemToWatchlistAsync(WatchlistItem item);
		Task<WatchlistItem?> GetItemByIdAsync(int itemId);
		Task<WatchlistItem> UpdateWatchlistItemAsync(WatchlistItem item);
		Task RemoveItemFromWatchlistAsync(int itemId);
		Task UpdateCurrentPriceBySymbolAsync(string symbol, decimal currentPrice);
	}
}
