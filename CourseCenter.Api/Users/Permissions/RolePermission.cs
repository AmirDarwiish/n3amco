public class RolePermission
{
    public int Id { get; set; }
    public string Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
