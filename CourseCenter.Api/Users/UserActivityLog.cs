    namespace CourseCenter.Api.Users
{
    public class UserActivityLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ActivityType { get; set; } = null!;
        // Login / Logout / Action

        public string? EntityName { get; set; }
        // Lead / Student / Payment / Enrollment

        public string? ActionName { get; set; }
        // Create / Update / Delete / Assign

        public int? EntityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
