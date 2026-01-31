namespace CourseCenter.Api.Leads
{
    public class LeadTask
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;
        public string Title { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public string? Status { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
