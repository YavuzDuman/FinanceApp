using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Shared.Authorization
{
    public class ManagerAuthorizationHandler : AuthorizationHandler<ManagerAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ManagerAuthorizationRequirement requirement)
        {
            // Manager veya Admin rolü kontrolü
            var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            
            if (userRoles.Contains("Manager") || userRoles.Contains("Admin"))
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

    public class ManagerAuthorizationRequirement : IAuthorizationRequirement
    {
        // Manager yetkisi gerektiren işlemler için
    }
}
