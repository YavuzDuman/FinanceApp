using Shared.Abstract;

namespace UserService.Entities.Dto
{
	public class RegisterDto : IDto
	{
		public string Name { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
	}
}
