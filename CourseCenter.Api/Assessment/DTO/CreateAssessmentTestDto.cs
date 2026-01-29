using System.ComponentModel.DataAnnotations;

namespace CourseCenter.Api.Assessment.DTO
{
    public class CreateAssessmentTestDto
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Range(1, 300)]
        public int DurationMinutes { get; set; }
    }

}
