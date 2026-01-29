namespace CourseCenter.Api.Assessment
{
    public class AssessmentAttempt
    {
        public int Id { get; set; }
        public int TestId { get; set; }

        public int? LeadId { get; set; }
        public int? StudentId { get; set; }

        public int TotalScore { get; set; }
        public string? ResultLabel { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
    }

}
