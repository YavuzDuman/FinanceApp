using Shared.Abstract;

namespace UserService.Entities.Concrete
{
	public class User : IEntity
	{
		public int UserId { get; set; }
		public string Name { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public DateTime InsertDate { get; set; }

		public ICollection<UserRole>? UserRoles { get; set; }
	}
}
