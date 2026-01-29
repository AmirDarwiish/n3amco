using CourseCenter.Api.Assessment.DTO;

namespace CourseCenter.Api.Assessment.Services
{
    public interface IAssessmentService
    {
        SubmitAssessmentResult SubmitAssessment(
            int attemptId,
            SubmitAssessmentDto dto);
    }
}
