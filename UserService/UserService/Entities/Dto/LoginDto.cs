using Shared.Abstract;

namespace UserService.Entities.Dto
{
	public class LoginDto : IDto
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}
}
