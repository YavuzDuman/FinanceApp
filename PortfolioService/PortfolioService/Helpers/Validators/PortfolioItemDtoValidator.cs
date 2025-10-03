using FluentValidation;
using PortfolioService.Entities.Dtos;

namespace PortfolioService.Helpers.Validators
{
	public class PortfolioItemDtoValidator : AbstractValidator<PortfolioItemDto>
	{
		public PortfolioItemDtoValidator()
		{
			RuleFor(x=>x.Symbol).NotEmpty().WithMessage("Sembol boş olamaz.")
				.MaximumLength(10).WithMessage("Sembol en fazla 10 karakter olabilir.");

			RuleFor(x => x.Quantity).NotEmpty().WithMessage("Miktar boş olamaz.")
				.GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.");
			RuleFor(x=>x.AverageCost).NotEmpty().WithMessage("Ortalama maliyet boş olamaz.")
				.GreaterThan(0).WithMessage("Ortalama maliyet sıfırdan büyük olmalıdır.");
		}
	}
}