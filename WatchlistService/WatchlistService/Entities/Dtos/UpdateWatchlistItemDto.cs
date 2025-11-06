using Shared.Abstract;

namespace WatchlistService.Entities.Dtos
{
	public record UpdateWatchlistItemDto : IDto
	{
		public string? Note { get; set; }
	}
}
