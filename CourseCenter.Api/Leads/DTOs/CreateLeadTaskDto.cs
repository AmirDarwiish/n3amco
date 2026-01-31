namespace CourseCenter.Api.Leads.DTOs
{
    public class CreateLeadTaskDto
    {
        public string Title { get; set; } = null!;
        public DateTime? DueDate { get; set; }
    }
}
