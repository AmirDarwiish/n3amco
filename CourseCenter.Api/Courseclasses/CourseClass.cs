using CourseCenter.Api.Courseclasses;
using CourseCenter.Api.Enrollments;

namespace CourseCenter.Api.Courses
{
    public class CourseClass
    {
        public int Id { get; set; }

        // Relation
        public int CourseId { get; set; }
        public Course Course { get; set; }

        // Display
        public string Name { get; set; }          // Morning / Evening
        public string Code { get; set; }          // FLUT-2026-01
        public decimal Price { get; set; }


        // Instructor (temporary)
        public string InstructorName { get; set; }

        // Schedule
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string DaysOfWeek { get; set; }    // "Sat,Mon,Wed"
        public TimeSpan TimeFrom { get; set; }
        public TimeSpan TimeTo { get; set; }

        // Capacity
        public int? MaxStudents { get; set; }

        // Status
        public ClassStatus Status { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }


        // Navigation
        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
