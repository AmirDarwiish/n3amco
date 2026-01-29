namespace CourseCenter.Api.Assessment
{
    public class AssessmentUserAnswer
    {
        public int Id { get; set; }
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public int AnswerId { get; set; }

        public int Value { get; set; }
        public int Score { get; set; }
    }

}
