using CourseCenter.Api.Controllers;
using System.Collections.Generic;

namespace CourseCenter.Api.Assessment.DTO
{
    public class SubmitAssessmentDto
    {
        public List<SubmitAnswerDto> Answers { get; set; }
    }
}
