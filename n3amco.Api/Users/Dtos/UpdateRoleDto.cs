namespace n3amco.Api.Users.Dtos
{
    public class UpdateRoleDto
    {
        public string RoleName { get; set; } = null!;
        public List<string> PermissionCodes { get; set; } = new();
    }
}
