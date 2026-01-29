namespace CourseCenter.Api.Assessment
{
    public class AssessmentResultRange
    {
        public int Id { get; set; }

        public int TestId { get; set; }   // مربوط بالامتحان
        public int FromScore { get; set; }
        public int ToScore { get; set; }

        public string ResultLabel { get; set; } // Beginner / Advanced
    }

}
