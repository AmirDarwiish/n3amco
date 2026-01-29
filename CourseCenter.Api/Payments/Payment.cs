using CourseCenter.Api.Enrollments;
using CourseCenter.Api.Users;

namespace CourseCenter.Api.Payments
{
    public class Payment
    {
        public int Id { get; set; }

        public int EnrollmentId { get; set; }
        public Enrollment Enrollment { get; set; } = null!;

        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string? ReferenceNumber { get; set; }

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
    }
}
