using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public record UpdatePortfolioItemDto : IDto
	{
		public decimal NewPurchasePrice { get; set; }
		public int NewQuantity { get; set; }
	}
}
