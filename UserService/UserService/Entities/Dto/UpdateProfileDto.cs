using Shared.Abstract;

namespace UserService.Entities.Dto
{
	public record UpdateProfileDto : IDto
	{
		public string Name { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
	}
}

