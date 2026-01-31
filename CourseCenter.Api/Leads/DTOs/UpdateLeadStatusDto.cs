namespace CourseCenter.Api.Leads.DTOs
{
    public class UpdateLeadStatusDto
    {
        public LeadStatus Status { get; set; }
        public string? Reason { get; set; }
    }


}
