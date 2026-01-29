namespace CourseCenter.Api.Leads.DTOs
{
    public class LeadListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public LeadStatus Status { get; set; }
        public string Source { get; set; }
        public string AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
