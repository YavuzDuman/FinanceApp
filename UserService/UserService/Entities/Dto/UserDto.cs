using Shared.Abstract;

namespace UserService.Entities.Dto
{
	// ⚡ Immutable record - Performance iyileştirmesi
	public record UserDto(
		int UserId,
		string Name,
		string Username,
		string Email,
		string RoleName,
		DateTime RegistrationDate,
		bool IsActive
	) : IDto;
}
