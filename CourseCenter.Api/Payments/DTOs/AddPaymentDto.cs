namespace CourseCenter.Api.Payments.DTOs
{
    public class AddPaymentDto
    {
        public int EnrollmentId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
