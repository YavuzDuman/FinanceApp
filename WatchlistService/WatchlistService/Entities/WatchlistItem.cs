using Shared.Abstract;

namespace WatchlistService.Entities
{
	public class WatchlistItem: IEntity
	{
		public int Id { get; set; }
		public int WatchlistId { get; set; }
		public string Symbol { get; set; } = string.Empty;
		public string? Note { get; set; }
		public DateTime EntryDate { get; set; } = DateTime.UtcNow;
		public decimal CurrentPrice { get; set; }

		// Navigasyon Özelliği (Watchlist'e geri bağlantı)
		public Watchlist Watchlist { get; set; } = null!;
	}
}
