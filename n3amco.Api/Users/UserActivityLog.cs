    namespace DairySystem.Api.Users
{
    public class UserActivityLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ActivityType { get; set; } = null!;

        public string? EntityName { get; set; }

        public string? ActionName { get; set; }

        public int? EntityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
