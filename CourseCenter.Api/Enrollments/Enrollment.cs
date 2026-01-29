using CourseCenter.Api.Courseclasses;
using CourseCenter.Api.Courses;
using CourseCenter.Api.Payments;
using CourseCenter.Api.Students;

namespace CourseCenter.Api.Enrollments
{
    public class Enrollment
    {
        public int Id { get; set; }

        // =========================
        // Student
        // =========================
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        // =========================
        // Course Class (بدل Course)
        // =========================
        public int CourseClassId { get; set; }
        public CourseClass CourseClass { get; set; } = null!;

        // =========================
        // Payment
        // =========================
        public decimal CoursePrice { get; set; }

        public PaymentProgressStatus PaymentProgressStatus { get; set; }
            = PaymentProgressStatus.NotPaid;

        // =========================
        // Status
        // =========================
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    }
}
