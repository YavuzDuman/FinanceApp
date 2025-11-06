using Shared.Abstract;

namespace WatchlistService.Entities.Dtos
{
	public record WatchlistResponseDto : IDto
	{
		public int Id { get; set; }
		public string ListName { get; set; }
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; }
		public List<WatchlistItemResponseDto> Items { get; set; } = new List<WatchlistItemResponseDto>();
	}
}
