namespace DairySystem.Api.Users.Auth.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public UserInfoDto User { get; set; } = null!;
    }

    public class UserInfoDto
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public List<string> Permissions { get; set; } = new();
    }
}
