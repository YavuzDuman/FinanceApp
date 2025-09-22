using Shared.Abstract;

namespace PortfolioService.Entities.Dtos
{
	public class CreatePortfolioDto : IDto
	{
		public string Name { get; set; }
	}
}
