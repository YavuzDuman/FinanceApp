using FluentValidation;
using PortfolioService.Entities.Dtos;
using System.Data;

namespace PortfolioService.Helpers.Validators
{
	public class AddItemToPortfolioDtoValidator : AbstractValidator<AddItemToPortfolioDto>
	{
		public AddItemToPortfolioDtoValidator() 
		{
			RuleFor(x=>x.Symbol).NotEmpty().WithMessage("Symbol boş olamaz.");

			RuleFor(x=>x.Quantity).NotEmpty().WithMessage("Quantity boş olamaz.")
				.GreaterThan(0).WithMessage("Quantity 0'dan büyük olmalıdır.");

			RuleFor(x => x.PurchasePrice).NotEmpty().WithMessage("Quantity boş olamaz.");
		}
	}


}
