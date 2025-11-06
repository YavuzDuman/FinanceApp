using FluentValidation;
using UserService.Entities.Dto;

namespace UserService.Entities.Validators
{
	public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
	{
		public UpdateProfileDtoValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Ad soyad boş olamaz.")
				.MinimumLength(2).WithMessage("Ad soyad en az 2 karakter olmalıdır.")
				.MaximumLength(100).WithMessage("Ad soyad en fazla 100 karakter olabilir.");

			RuleFor(x => x.Username)
				.NotEmpty().WithMessage("Kullanıcı adı boş olamaz.")
				.MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.")
				.MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.")
				.Matches("^[a-zA-Z0-9_]+$").WithMessage("Kullanıcı adı sadece harf, rakam ve alt çizgi içerebilir.");

			RuleFor(x => x.Email)
				.NotEmpty().WithMessage("E-posta boş olamaz.")
				.EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
				.MaximumLength(100).WithMessage("E-posta en fazla 100 karakter olabilir.");
		}
	}
}

