using CourseCenter.Api.Payments;
using CourseCenter.Api.Payments.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseCenter.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ➕ Add Payment
        [HttpPost]
        [Authorize(Policy = "PAYMENTS_CREATE")]
        public IActionResult Add(AddPaymentDto dto)
        {
            var enrollment = _context.Enrollments
                .FirstOrDefault(e => e.Id == dto.EnrollmentId);

            if (enrollment == null)
                return BadRequest("Enrollment not found");

            var totalPaid = _context.Payments
                .Where(p => p.EnrollmentId == dto.EnrollmentId &&
                            p.Status == PaymentStatus.Paid)
                .Sum(p => p.Amount);

            var remainingAmount = enrollment.CoursePrice - totalPaid;

            if (dto.Amount <= 0)
                return BadRequest("Invalid payment amount");

            if (dto.Amount > remainingAmount)
                return BadRequest(new
                {
                    message = "Payment exceeds remaining amount",
                    remaining = remainingAmount
                });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var payment = new Payment
            {
                EnrollmentId = dto.EnrollmentId,
                Amount = dto.Amount,
                Method = dto.Method,
                CreatedByUserId = userId,
                ReferenceNumber = dto.ReferenceNumber
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();


            return Ok("Payment added successfully");
        }
    }
}
