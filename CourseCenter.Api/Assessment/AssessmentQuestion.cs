namespace CourseCenter.Api.Assessment
{
    public class AssessmentQuestion
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public AssessmentTest Test { get; set; }

        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public int Order { get; set; }
        public ICollection<AssessmentAnswer> Answers { get; set; }

    }

}
