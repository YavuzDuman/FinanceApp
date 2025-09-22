using Shared.Abstract;

namespace UserService.Entities.Dto
{
	public class UserDto : IDto
	{
		public int UserId { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string RoleName { get; set; }
		public DateTime InsertDate { get; set; }
	}
}
