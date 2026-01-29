using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CourseCenter.Api.Users.Authorization
{
    public class PermissionHandler
        : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ApplicationDbContext _context;

        public PermissionHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {

            // Check all role claims (user may have multiple roles)
            var roles = context.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();

            if (!roles.Any())
                return Task.CompletedTask;

            var hasPermission = _context.RolePermissions
                .Any(rp =>
                    roles.Contains(rp.Role) &&
                    rp.Permission.Code == requirement.PermissionCode
                );

            if (hasPermission)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
