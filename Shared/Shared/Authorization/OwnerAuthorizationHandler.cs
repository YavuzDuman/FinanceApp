using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Shared.Authorization
{
    public class OwnerAuthorizationHandler : AuthorizationHandler<OwnerAuthorizationRequirement, object>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerAuthorizationRequirement requirement, object resource)
        {
            // 1. JWT token'dan kullanıcının ID'sini (claim) al.
            var userIdClaim = context.User.FindFirst("sub") ?? 
                              context.User.FindFirst(ClaimTypes.NameIdentifier);

            // Eğer token'da kullanıcı ID'si yoksa veya null ise, yetkilendirme başarısız.
            if (userIdClaim == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // 2. Resource'tan UserId'yi al (reflection ile)
            var userIdProperty = resource.GetType().GetProperty("UserId");
            if (userIdProperty != null)
            {
                var resourceUserId = userIdProperty.GetValue(resource)?.ToString();
                
                if (resourceUserId == userIdClaim.Value)
                {
                    // Eğer ID'ler eşleşiyorsa, yani kullanıcı kendi kaynağını manipüle ediyorsa, yetkilendirme başarılı.
                    context.Succeed(requirement);
                }
                else
                {
                    // ID'ler eşleşmiyorsa, yetkilendirme başarısız.
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }

    public class OwnerAuthorizationRequirement : IAuthorizationRequirement
    {
        // Owner yetkisi gerektiren işlemler için
        // Bu requirement, kullanıcının sadece kendi kaynaklarını manipüle edebilmesini sağlar
    }
}
