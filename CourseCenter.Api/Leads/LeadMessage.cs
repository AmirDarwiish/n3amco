namespace CourseCenter.Api.Leads
{
    public class LeadMessage
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;
        public string? Channel { get; set; }
        public string? MessagePreview { get; set; }
        public string? Direction { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
