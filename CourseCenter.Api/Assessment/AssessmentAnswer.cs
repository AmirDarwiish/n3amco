using CourseCenter.Api.Assessment;

public class AssessmentAnswer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }
    public AssessmentQuestion Question { get; set; } 

    public string Text { get; set; }
    public int Score { get; set; }
}
