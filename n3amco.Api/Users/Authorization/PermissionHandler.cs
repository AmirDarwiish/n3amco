using n3amco.Api.Common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace n3amco.Api.Users.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // 🔥 SuperAdmin bypass
            if (context.User.IsInRole("SuperAdmin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var hasPermission = context.User.Claims
                .Where(c => c.Type == "permission") // 👈 أهم سطر
                .Any(c => c.Value == requirement.PermissionCode);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                throw new AppException(
                    $"Missing Permission: {requirement.PermissionCode}",
                    403
                );
            }

            return Task.CompletedTask;
        }
    }
}