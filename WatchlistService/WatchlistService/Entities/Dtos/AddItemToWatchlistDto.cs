using Shared.Abstract;

namespace WatchlistService.Entities.Dtos
{
	public record AddItemToWatchlistDto : IDto
	{
		public string Symbol { get; set; }
		public string? Note { get; set; }
	}
}
