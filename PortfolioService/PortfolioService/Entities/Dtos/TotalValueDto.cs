using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public record TotalValueDto : IDto
	{
		public decimal TotalValue { get; set; }
		public decimal TotalCost { get; set; }
	}
}
