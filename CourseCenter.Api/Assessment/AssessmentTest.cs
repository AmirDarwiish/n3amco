namespace CourseCenter.Api.Assessment
{
    public class AssessmentTest
    {
        public string? PublicKey { get; set; }
        public bool IsPublic { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }     
        public int DurationMinutes { get; set; }      
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
