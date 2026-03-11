using Microsoft.AspNetCore.Authorization;

namespace DairySystem.Api.Users.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionCode { get; }

        public PermissionRequirement(string permissionCode)
        {
            PermissionCode = permissionCode;
        }
    }
}
