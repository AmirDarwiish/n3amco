namespace CourseCenter.Api.Assessment.DTO
{
    public class CreateQuestionDto
    {
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public int Order { get; set; }
    }

}
