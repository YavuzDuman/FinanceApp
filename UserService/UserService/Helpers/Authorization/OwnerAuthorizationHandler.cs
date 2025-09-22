using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UserService.Entities.Concrete;

namespace WebApi.Helpers.Authorization
{
	public class OwnerAuthorizationHandler : AuthorizationHandler<OwnerAuthorizationRequirement, User>
	{
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerAuthorizationRequirement requirement, User resource)
		{
			// 1. JWT token'dan kullanıcının ID'sini (claim) al.
			var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

			// Eğer token'da kullanıcı ID'si yoksa veya null ise, yetkilendirme başarısız.
			if (userIdClaim == null)
			{
				context.Fail();
				return Task.CompletedTask;
			}

			// 2. Güncellenmek veya silinmek istenen kaynağın (User) ID'si ile token'daki ID'yi karşılaştır.
			if (resource.UserId.ToString() == userIdClaim.Value)
			{
				// Eğer ID'ler eşleşiyorsa, yani kullanıcı kendi kaynağını manipüle ediyorsa, yetkilendirme başarılı.
				context.Succeed(requirement);
			}
			else
			{
				// ID'ler eşleşmiyorsa, yetkilendirme başarısız.
				context.Fail();
			}

			return Task.CompletedTask;
		}
	}
}
