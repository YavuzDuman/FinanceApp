using Shared.Abstract;

namespace WatchlistService.Entities
{
	public class Watchlist : IEntity
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public string ListName { get; set; }
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; }

		public ICollection<WatchlistItem> Items { get; set; } = new List<WatchlistItem>();

	}
}
