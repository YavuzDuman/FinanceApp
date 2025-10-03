using Microsoft.AspNetCore.Authorization;

namespace Shared.Authorization
{
    public class RoleAuthorizationRequirement : IAuthorizationRequirement
    {
        public string[] RequiredRoles { get; }

        public RoleAuthorizationRequirement(params string[] requiredRoles)
        {
            RequiredRoles = requiredRoles ?? throw new ArgumentNullException(nameof(requiredRoles));
        }
    }
}
