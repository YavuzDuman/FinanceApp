using FluentValidation;
using NoteService.Entities;

namespace NoteService.Helpers.Validators
{
	public class NoteDtoValidator : AbstractValidator<NoteDto>
	{
		public NoteDtoValidator()
		{
			RuleFor(x => x.Content).NotEmpty().WithMessage("Not içeriği boş bırakılamaz.");

			RuleFor(x => x.StockSymbol).NotEmpty().WithMessage("Sembol ismi boş bırakılamaz.");
		}
	}
}
