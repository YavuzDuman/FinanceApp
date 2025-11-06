using Shared.Abstract;

namespace UserService.Entities.Dto
{
	public record RefreshTokenRequestDto : IDto
	{
		public string RefreshToken { get; set; }
	}
}

