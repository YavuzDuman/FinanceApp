using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Shared.Authorization
{
    public class RoleAuthorizationHandler : AuthorizationHandler<RoleAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleAuthorizationRequirement requirement)
        {
            // Kullanıcının rollerini al
            var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            // Gerekli rollerden en az birine sahip mi kontrol et
            if (requirement.RequiredRoles.Any(role => userRoles.Contains(role)))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
