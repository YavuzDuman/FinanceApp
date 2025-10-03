using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public class UpdatePortfolioItemDto : IDto
	{
		public decimal NewPurchasePrice { get; set; }
		public int NewQuantity { get; set; }
	}
}
