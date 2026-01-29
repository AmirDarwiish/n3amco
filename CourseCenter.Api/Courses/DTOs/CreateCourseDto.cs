using CourseCenter.Api.Courses;

public class CreateCourseDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Description { get; set; } = null!;

    public int? EstimatedDurationHours { get; set; }
    public CourseLevel Level { get; set; }

    public string? LearningOutcomes { get; set; }
    public string? Prerequisites { get; set; }
    public string? Language { get; set; }
    public string? Tags { get; set; }
    public string? ThumbnailUrl { get; set; }

    public int CategoryId { get; set; }
}
