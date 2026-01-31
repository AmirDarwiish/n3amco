namespace CourseCenter.Api.Leads.DTOs
{
    public class UpdateLeadNoteDto
    {
        public string Note { get; set; }
        public CourseCenter.Api.Leads.LeadInteractionType? InteractionType { get; set; }
    }

}
