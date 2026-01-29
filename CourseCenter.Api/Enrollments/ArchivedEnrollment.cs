namespace CourseCenter.Api.Enrollments
{
    public class ArchivedEnrollment
    {
        public int Id { get; set; }

        public int OriginalEnrollmentId { get; set; }

        public int StudentId { get; set; }

        // ✅ بدل CourseId
        public int CourseClassId { get; set; }

        public EnrollmentStatus Status { get; set; }
        public DateTime EnrollmentDate { get; set; }

        public DateTime ArchivedAt { get; set; }
        public int ArchivedByUserId { get; set; }
    }
}
