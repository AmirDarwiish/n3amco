namespace CourseCenter.Api.Leads
{
    public class LeadTagLink
    {
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;
        public int TagId { get; set; }
        public LeadTag Tag { get; set; } = null!;
    }
}
