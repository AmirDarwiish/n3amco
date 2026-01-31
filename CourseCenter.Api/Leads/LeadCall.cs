namespace CourseCenter.Api.Leads
{
    public class LeadCall
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;
        public int? DurationInMinutes { get; set; }
        public string? CallResult { get; set; }
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
