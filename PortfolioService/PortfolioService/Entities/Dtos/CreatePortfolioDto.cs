using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public record CreatePortfolioDto : IDto
	{
		public string Name { get; set; }
	}
}
