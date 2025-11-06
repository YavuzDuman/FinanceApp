using Shared.Abstract;

namespace UserService.Entities.Concrete
{
	public class RefreshToken : IEntity
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public string Token { get; set; }
		public DateTime ExpiryDate { get; set; }
		public DateTime CreatedDate { get; set; }
		public bool IsRevoked { get; set; }
		public string? RevokedByIp { get; set; }
		public DateTime? RevokedDate { get; set; }
		
		// Navigation Property
		public User User { get; set; }
	}
}

