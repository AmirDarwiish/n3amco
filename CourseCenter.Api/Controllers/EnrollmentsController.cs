using CourseCenter.Api.Enrollments;
using CourseCenter.Api.Enrollments.DTOs;
using CourseCenter.Api.Courseclasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CourseCenter.Api.Common;

namespace CourseCenter.Api.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // POST: api/enrollments
        // =========================
        [HttpPost]
        [Authorize(Policy = "ENROLLMENTS_CREATE")]
        public IActionResult Enroll(CreateEnrollmentDto dto)
        {
            // 1️⃣ Student exists
            if (!_context.Students.Any(s => s.Id == dto.StudentId))
                return BadRequest("Invalid student");

            // 2️⃣ CourseClass exists + Open
            var courseClass = _context.CourseClasses
                .FirstOrDefault(c => c.Id == dto.CourseClassId);

            if (courseClass == null)
                return BadRequest("Invalid class");

            if (courseClass.Status != ClassStatus.Open)
                return BadRequest("Class is not open for enrollment");

            // 3️⃣ Capacity check
            var enrolledCount = _context.Enrollments
                .Count(e => e.CourseClassId == dto.CourseClassId &&
                            e.Status == EnrollmentStatus.Active);

            if (courseClass.MaxStudents.HasValue &&
                enrolledCount >= courseClass.MaxStudents.Value)
                return BadRequest("Class is full");

            // 4️⃣ Already enrolled in same class
            var alreadyEnrolled = _context.Enrollments.Any(e =>
                e.StudentId == dto.StudentId &&
                e.CourseClassId == dto.CourseClassId &&
                e.Status == EnrollmentStatus.Active);

            if (alreadyEnrolled)
                return BadRequest("Student already enrolled in this class");

            // 5️⃣ Create enrollment
            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                CourseClassId = dto.CourseClassId,
                Status = EnrollmentStatus.Active,
                EnrollmentDate = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            _context.SaveChanges();

            return Ok("Student enrolled successfully");
        }

        // =========================
        // GET: api/enrollments
        // =========================
        [HttpGet]
        [Authorize(Policy = "ENROLLMENTS_VIEW")]
        public IActionResult GetAll([FromQuery] EnrollmentStatus? status, [FromQuery] PagedRequest request)
        {
            if (request == null)
                request = new PagedRequest { PageNumber = 1, PageSize = 10 };

            if (request.PageNumber <= 0)
                request.PageNumber = 1;

            if (request.PageSize <= 0)
                request.PageSize = 10;

            request.PageSize = Math.Min(request.PageSize, 100);

            var query = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.CourseClass)
                    .ThenInclude(cc => cc.Course)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(e => e.Status == status.Value);

            // total before pagination
            var totalCount = query.Count();

            var dataAnon = query
                .OrderByDescending(e => e.EnrollmentDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new
                {
                    e.Id,
                    StudentName = e.Student.FullName,
                    CourseName = e.CourseClass.Course.Name,
                    ClassName = e.CourseClass.Name,
                    Status = e.Status.ToString(),
                    e.EnrollmentDate
                })
                .ToList();

            var data = dataAnon.Cast<object>().ToList();

            var response = new PagedResponse<object>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return Ok(response);
        }

        // =========================
        // GET: api/enrollments/{id}
        // =========================
        [HttpGet("{id:int}")]
        [Authorize(Policy = "ENROLLMENTS_VIEW")]
        public IActionResult GetById(int id)
        {
            var enrollment = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.CourseClass)
                    .ThenInclude(cc => cc.Course)
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    e.Id,
                    e.StudentId,
                    StudentName = e.Student.FullName,
                    e.CourseClassId,
                    ClassName = e.CourseClass.Name,
                    CourseName = e.CourseClass.Course.Name,
                    e.Status,
                    e.EnrollmentDate
                })
                .FirstOrDefault();

            if (enrollment == null)
                return NotFound("Enrollment not found");

            return Ok(enrollment);
        }

        // =========================
        // PUT: api/enrollments/{id}
        // =========================
        [HttpPut("{id:int}")]
        [Authorize(Policy = "ENROLLMENTS_EDIT")]
        public IActionResult Update(int id, UpdateEnrollmentDto dto)
        {
            var enrollment = _context.Enrollments.FirstOrDefault(e => e.Id == id);
            if (enrollment == null)
                return NotFound("Enrollment not found");

            if (enrollment.Status == EnrollmentStatus.Completed ||
                enrollment.Status == EnrollmentStatus.Cancelled)
                return BadRequest("Cannot edit completed or cancelled enrollment");

            if (!_context.Students.Any(s => s.Id == dto.StudentId))
                return BadRequest("Invalid student");

            var courseClass = _context.CourseClasses
                .FirstOrDefault(c => c.Id == dto.CourseClassId);

            if (courseClass == null || courseClass.Status != ClassStatus.Open)
                return BadRequest("Invalid or closed class");

            enrollment.StudentId = dto.StudentId;
            enrollment.CourseClassId = dto.CourseClassId;

            _context.SaveChanges();

            return Ok("Enrollment updated successfully");
        }

        // =========================
        // PUT: api/enrollments/{id}/status
        // =========================
        [HttpPut("{id:int}/status")]
        [Authorize(Policy = "ENROLLMENTS_CHANGE_STATUS")]
        public IActionResult UpdateStatus(int id, UpdateEnrollmentStatusDto dto)
        {
            var enrollment = _context.Enrollments.FirstOrDefault(e => e.Id == id);
            if (enrollment == null)
                return NotFound("Enrollment not found");

            if (enrollment.Status == EnrollmentStatus.Cancelled)
                return BadRequest("Cancelled enrollment cannot be updated");

            enrollment.Status = dto.Status;
            _context.SaveChanges();

            return Ok("Status updated successfully");
        }

        // =========================
        // PUT: api/enrollments/{id}/cancel
        // =========================
        [HttpPut("{id:int}/cancel")]
        [Authorize(Policy = "ENROLLMENTS_CANCEL")]
        public IActionResult Cancel(int id)
        {
            var enrollment = _context.Enrollments.FirstOrDefault(e => e.Id == id);
            if (enrollment == null)
                return NotFound("Enrollment not found");

            if (enrollment.Status == EnrollmentStatus.Completed)
                return BadRequest("Completed enrollment cannot be cancelled");

            enrollment.Status = EnrollmentStatus.Cancelled;
            _context.SaveChanges();

            return Ok("Enrollment cancelled successfully");
        }

        // =========================
        // PUT: api/enrollments/{id}/complete
        // =========================
        [HttpPut("{id:int}/complete")]
        [Authorize(Policy = "ENROLLMENTS_COMPLETE")]
        public IActionResult Complete(int id)
        {
            var enrollment = _context.Enrollments.FirstOrDefault(e => e.Id == id);
            if (enrollment == null)
                return NotFound("Enrollment not found");

            if (enrollment.Status != EnrollmentStatus.Active)
                return BadRequest("Only active enrollments can be completed");

            enrollment.Status = EnrollmentStatus.Completed;
            _context.SaveChanges();

            return Ok("Enrollment completed successfully");
        }

        // =========================
        // DELETE: api/enrollments/{id}
        // =========================
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "ENROLLMENTS_DELETE")]
        public IActionResult Archive(int id)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var enrollment = _context.Enrollments.FirstOrDefault(e => e.Id == id);
                if (enrollment == null)
                    return NotFound("Enrollment not found");

                if (enrollment.Status == EnrollmentStatus.Completed)
                    return BadRequest("Completed enrollment cannot be archived");

                var archived = new ArchivedEnrollment
                {
                    OriginalEnrollmentId = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    CourseClassId = enrollment.CourseClassId,
                    Status = enrollment.Status,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    ArchivedAt = DateTime.UtcNow,
                    ArchivedByUserId = 1 // TODO: from token
                };

                _context.ArchivedEnrollments.Add(archived);
                _context.Enrollments.Remove(enrollment);

                _context.SaveChanges();
                transaction.Commit();

                return Ok("Enrollment archived successfully");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =========================
        // GET: api/enrollments/archived
        // =========================
        [HttpGet("archived")]
        [Authorize(Policy = "ENROLLMENTS_VIEW")]
        public IActionResult GetArchived()
        {
            var archived = _context.ArchivedEnrollments
                .Select(a => new
                {
                    a.Id,
                    a.OriginalEnrollmentId,
                    a.StudentId,
                    a.CourseClassId,
                    a.Status,
                    a.EnrollmentDate,
                    a.ArchivedAt
                })
                .ToList();

            return Ok(archived);
        }

        // =========================
        // POST: api/enrollments/archived/{id}/restore
        // =========================
        [HttpPost("archived/{id:int}/restore")]
        [Authorize(Policy = "ENROLLMENTS_EDIT")]
        public IActionResult Restore(int id)
        {
            var archived = _context.ArchivedEnrollments.FirstOrDefault(a => a.Id == id);
            if (archived == null)
                return NotFound("Archived enrollment not found");

            var enrollment = new Enrollment
            {
                StudentId = archived.StudentId,
                CourseClassId = archived.CourseClassId,
                Status = archived.Status,
                EnrollmentDate = archived.EnrollmentDate
            };

            _context.Enrollments.Add(enrollment);
            _context.ArchivedEnrollments.Remove(archived);

            _context.SaveChanges();

            return Ok("Enrollment restored successfully");
        }
    }
}
