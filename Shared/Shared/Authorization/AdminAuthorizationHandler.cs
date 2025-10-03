using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Shared.Authorization
{
    public class AdminAuthorizationHandler : AuthorizationHandler<AdminAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminAuthorizationRequirement requirement)
        {
            // Admin rolü kontrolü
            var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            
            if (userRoles.Contains("Admin"))
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

    public class AdminAuthorizationRequirement : IAuthorizationRequirement
    {
        // Admin yetkisi gerektiren işlemler için
    }
}
