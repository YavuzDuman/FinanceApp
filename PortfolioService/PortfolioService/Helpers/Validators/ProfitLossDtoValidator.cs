using FluentValidation;
using PortfolioService.Entities.Dtos;

namespace PortfolioService.Helpers.Validators
{
	public class ProfitLossDtoValidator : AbstractValidator<ProfitLossDto>
	{
		public ProfitLossDtoValidator()
		{
			//RuleFor(x => x.Symbol).NotEmpty().WithMessage("Sembol alanı boş olamaz.");
		}
	}
}
