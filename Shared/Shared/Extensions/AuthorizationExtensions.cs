using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shared.Authorization;

namespace Shared.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddCentralizedAuthorization(this IServiceCollection services)
        {
            // Authorization Handler'ları Dependency Injection'a kaydet
            services.AddSingleton<IAuthorizationHandler, OwnerAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, RoleAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, AdminAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, ManagerAuthorizationHandler>();

            // Authorization servisini ekle
            services.AddAuthorization(options =>
            {
                // Admin sadece policy
                options.AddPolicy("AdminOnly", policy =>
                    policy.Requirements.Add(new AdminAuthorizationRequirement()));

                // Manager veya Admin policy
                options.AddPolicy("AdminOrManager", policy =>
                    policy.Requirements.Add(new RoleAuthorizationRequirement("Admin", "Manager")));

                // Owner authorization policy
                options.AddPolicy("OwnerOnly", policy =>
                    policy.Requirements.Add(new OwnerAuthorizationRequirement()));
            });

            return services;
        }
    }
}
