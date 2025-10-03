using FluentValidation;
using NoteService.Entities;

namespace NoteService.Helpers.Validators
{
	public class NoteValidator : AbstractValidator<Note>
	{
		public NoteValidator()
		{
			RuleFor(x => x.Content).NotEmpty().WithMessage("Not içeriği boş bırakılamaz.");

			RuleFor(x => x.StockSymbol).NotEmpty().WithMessage("Sembol ismi boş bırakılamaz.");
		}
	}
}
