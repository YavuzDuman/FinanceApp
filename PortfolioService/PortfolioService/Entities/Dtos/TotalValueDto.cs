using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public class TotalValueDto : IDto
	{
		public string Symbol { get; set; }
		public decimal TotalValue { get; set; }

	}
}
