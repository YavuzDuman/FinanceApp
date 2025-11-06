using FluentValidation;
using UserService.Entities.Dto;

namespace WebApi.Helpers.Validator
{
	public class RegisterDtoValidator : AbstractValidator<RegisterDto>
	{
		public RegisterDtoValidator()
		{
			RuleFor(x => x.Username)
				.NotEmpty().WithMessage("Kullanıcı adı boş olamaz.");

			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("E-posta adresi boş olamaz.");

			RuleFor(x => x.Password)
				.NotEmpty().WithMessage("Şifre boş olamaz.").MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

			RuleFor(x => x.Email)
				.EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
		}
	}
}
