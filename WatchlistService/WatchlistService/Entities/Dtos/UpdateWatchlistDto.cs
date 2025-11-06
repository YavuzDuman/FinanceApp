using Shared.Abstract;

namespace WatchlistService.Entities.Dtos
{
	public record UpdateWatchlistDto : IDto
	{
		public string? ListName { get; set; }
		public string? Description { get; set; }
	}
}
