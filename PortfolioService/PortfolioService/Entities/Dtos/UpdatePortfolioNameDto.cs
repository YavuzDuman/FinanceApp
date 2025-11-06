using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public record UpdatePortfolioNameDto : IDto
	{
		public string NewName { get; set; }
	}
}
