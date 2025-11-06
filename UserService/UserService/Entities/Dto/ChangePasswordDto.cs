using System.ComponentModel.DataAnnotations;

namespace UserService.Entities.Dto
{
	public record ChangePasswordDto
	{
		// Admin başkasının şifresini değiştirirken bu alan null olabilir
		public string? OldPassword { get; set; }

		[Required(ErrorMessage = "Yeni şifre gereklidir")]
		[MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
		public string NewPassword { get; set; } = string.Empty;
	}
}
