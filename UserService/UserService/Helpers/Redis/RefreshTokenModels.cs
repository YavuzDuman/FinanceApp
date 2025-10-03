using System;

namespace UserService.Helpers.Redis
{
	public record RefreshTokenRecord(
		string TokenId,
		int UserId,
		string TokenHash,
		DateTime CreatedAtUtc,
		DateTime ExpiresAtUtc,
		string? Ip,
		string? UserAgent);
}


