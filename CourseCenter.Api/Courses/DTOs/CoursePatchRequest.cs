using CourseCenter.Api.Courses;

namespace CourseCenter.Api.Courses.DTOs
{
    public class CoursePatchRequest
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public int? EstimatedDurationHours { get; set; }
        public CourseLevel? Level { get; set; }

        public string? Language { get; set; }
        public string? Tags { get; set; }
        public string? LearningOutcomes { get; set; }
        public string? Prerequisites { get; set; }
        public string? ThumbnailUrl { get; set; }

        public int? CategoryId { get; set; }
    }

}
