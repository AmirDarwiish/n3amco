using CourseCenter.Api.Courses;

namespace CourseCenter.Api.Courses.DTOs
{
    public class UpdateCourseDto
    {
        // Identity
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;

        // Content
        public int? EstimatedDurationHours { get; set; }
        public CourseLevel Level { get; set; }

        // Category
        public int CategoryId { get; set; }
    }
}
