namespace CourseCenter.Api.Leads
{
    public class LeadFollowUpLog
    {
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;

        public DateTime FollowUpDate { get; set; }
        public string Reason { get; set; } = null!;

        public FollowUpSource Source { get; set; } // Task / Manual

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
