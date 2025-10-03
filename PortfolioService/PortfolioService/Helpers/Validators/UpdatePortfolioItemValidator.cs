using FluentValidation;
using PortfolioService.Entities.Dtos;

namespace PortfolioService.Helpers.Validators
{
	public class UpdatePortfolioItemValidator : AbstractValidator<UpdatePortfolioItemDto>
	{
		public UpdatePortfolioItemValidator()
		{
			RuleFor(x => x.NewPurchasePrice)
				.GreaterThan(0).WithMessage("New purchase price must be greater than zero.");
			RuleFor(x => x.NewQuantity)
				.GreaterThan(0).WithMessage("New quantity must be greater than zero.");
		}
	}
}
