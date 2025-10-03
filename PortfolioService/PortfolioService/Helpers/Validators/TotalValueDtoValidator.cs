using FluentValidation;
using PortfolioService.Entities.Dtos;

namespace PortfolioService.Helpers.Validators
{
	public class TotalValueDtoValidator : AbstractValidator<TotalValueDto>
	{
		public TotalValueDtoValidator()
		{
			RuleFor(x => x.Symbol).NotEmpty().WithMessage("Sembol alanı boş bırakılamaz.");
		}
	}
}
