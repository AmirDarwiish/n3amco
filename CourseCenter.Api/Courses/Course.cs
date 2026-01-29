using CourseCenter.Api.Categories;
using CourseCenter.Api.Courseclasses;
using CourseCenter.Api.Courses;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourseCenter.Api.Courses
{
    public class Course
    {
        public int Id { get; set; }

        // Identity
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;

        // Content
        public int? EstimatedDurationHours { get; set; }
        public CourseLevel Level { get; set; } = CourseLevel.Beginner;
        public string? LearningOutcomes { get; set; }
        public string? Prerequisites { get; set; }
        public string? Language { get; set; }
        public string? Tags { get; set; }
        public string? ThumbnailUrl { get; set; }

        // Category
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // Relations
        public ICollection<CourseClass> Classes { get; set; } = new List<CourseClass>();

        // Calculated (not stored)
        [NotMapped]
        public int ClassesCount => Classes.Count;

        // Status & Audit
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}