using WatchlistService.Entities;

namespace WatchlistService.Business.Abstract
{
	public interface IWatchlistManager
	{
		Task<Watchlist?> GetWatchlistWithItemsAsync(int id);
		Task<List<Watchlist>> GetAllByUserIdAsync(int userId);
		Task<Watchlist> AddWatchlistAsync(int userId, string name,string? description);
		Task<Watchlist> UpdateWatchlistAsync(int watchlistId, int userId, string? newName, string? description);
		Task DeleteWatchlistAsync(int id, int userId);
		Task<WatchlistItem> AddItemToWatchlistAsync(int watchlistId, string symbol, int userId, HttpContext? httpContext = null, string? note = null);
		Task<WatchlistItem?> GetItemByIdAsync(int itemId);
		Task<WatchlistItem> UpdateWatchlistItemAsync(int itemId, string? note, int userId);
		Task RemoveItemFromWatchlistAsync(int itemId, int userId);
	}
}
