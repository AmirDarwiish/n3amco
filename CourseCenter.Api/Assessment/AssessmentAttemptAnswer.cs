namespace CourseCenter.Api.Assessment
{
    public class AssessmentAttemptAnswer
    {
        public int Id { get; set; }

        public int AttemptId { get; set; }
        public AssessmentAttempt Attempt { get; set; }

        public int QuestionId { get; set; }
        public int AnswerId { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
