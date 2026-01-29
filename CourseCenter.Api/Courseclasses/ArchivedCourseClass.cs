namespace CourseCenter.Api.Courseclasses
{
    public class ArchivedCourseClass
    {
        public int Id { get; set; }

        public int OriginalCourseClassId { get; set; }
        public int CourseId { get; set; }

        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public decimal Price { get; set; }
        public string? InstructorName { get; set; }

        public ClassStatus Status { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? DaysOfWeek { get; set; }
        public TimeSpan? TimeFrom { get; set; }
        public TimeSpan? TimeTo { get; set; }

        public int? MaxStudents { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ArchivedAt { get; set; }
        public int ArchivedByUserId { get; set; }
    }
}
