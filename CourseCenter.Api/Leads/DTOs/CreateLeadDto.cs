namespace CourseCenter.Api.Leads.DTOs
{
    public class CreateLeadDto
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string? Source { get; set; }
    }

}
