namespace CourseCenter.Api.Leads
{
    public class LeadMention
    {
        public int Id { get; set; }

        public int LeadNoteId { get; set; }
        public LeadNote LeadNote { get; set; } = null!;

        public int MentionedUserId { get; set; }
        public int MentionedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
