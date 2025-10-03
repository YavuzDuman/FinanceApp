using FluentValidation;
using PortfolioService.Entities.Dtos;

namespace PortfolioService.Helpers.Validators
{
	public class UpdatePortfolioNameDtoValidator : AbstractValidator<UpdatePortfolioNameDto>
	{
		public UpdatePortfolioNameDtoValidator()
		{
			RuleFor(x => x.NewName).NotEmpty().WithMessage("Yeni isim alanı boş olamaz.");
		}
	}
}
