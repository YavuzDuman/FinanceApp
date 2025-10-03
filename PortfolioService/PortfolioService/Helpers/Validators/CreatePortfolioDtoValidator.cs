using FluentValidation;
using PortfolioService.Entities.Dtos;

namespace PortfolioService.Helpers.Validators
{
	public class CreatePortfolioDtoValidator : AbstractValidator<CreatePortfolioDto>
	{
		public CreatePortfolioDtoValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Portföy adı boş olamaz.")
				.MaximumLength(100).WithMessage("Portföy adı en fazla 100 karakter olabilir.");
		}
	}
}

