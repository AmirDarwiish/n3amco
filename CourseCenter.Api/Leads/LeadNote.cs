using CourseCenter.Api.Users;

namespace CourseCenter.Api.Leads
{
    public class LeadNote
    {
        public int Id { get; set; }

        // Relations
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        // Note Content
        public string Note { get; set; } = null!;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
