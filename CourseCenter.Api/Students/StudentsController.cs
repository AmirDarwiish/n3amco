using CourseCenter.Api.Enrollments;
using CourseCenter.Api.Students.Dtos;
using CourseCenter.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseCenter.Api.Students
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: api/Students
        // =========================
        [HttpGet]
        public IActionResult GetAll([FromQuery] PagedRequest request)
        {
            if (request == null)
                request = new PagedRequest { PageNumber = 1, PageSize = 10 };

            if (request.PageNumber <= 0)
                request.PageNumber = 1;

            if (request.PageSize <= 0)
                request.PageSize = 10;

            request.PageSize = Math.Min(request.PageSize, 100);

            var query = _context.Students.AsQueryable();

            var totalCount = query.Count();

            var data = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var response = new PagedResponse<Student>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return Ok(response);
        }

        // =========================
        // GET: api/Students/{id}
        // =========================
        [HttpGet("{id:int}")]
        [Authorize(Policy = "STUDENTS_VIEW")]
        public IActionResult GetById(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == id);

            if (student == null)
                return NotFound("Student not found");

            return Ok(student);
        }

        // =========================
        // GET: api/Students/by-name?fullname=amir
        // =========================
        [HttpGet("by-name")]
        [Authorize(Policy = "STUDENTS_VIEW")]
        public IActionResult SearchByName([FromQuery] string fullname)
        {
            if (string.IsNullOrWhiteSpace(fullname))
                return Ok(new List<object>());

            var students = _context.Students
                .Where(s => s.FullName.Contains(fullname))
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.PhoneNumber,
                    s.Level
                })
                .ToList();

            return Ok(students);
        }

        // =========================
        // POST: api/Students
        // =========================
        [HttpPost]
        [Authorize(Policy = "STUDENTS_CREATE")]
        public IActionResult Create(CreateStudentRequest request)
        {
            // 🔒 Minimal validation – no response change
            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest("Full name is required");

            // 🔁 Prevent duplicates
            var exists = _context.Students.Any(s =>
                (!string.IsNullOrEmpty(request.Email) && s.Email == request.Email) ||
                (!string.IsNullOrEmpty(request.PhoneNumber) && s.PhoneNumber == request.PhoneNumber) ||
                (!string.IsNullOrEmpty(request.NationalId) && s.NationalId == request.NationalId)
            );

            if (exists)
                return BadRequest("Student already exists");

            var student = new Student
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                NationalId = request.NationalId,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,

                RelativeName = request.RelativeName,
                ParentPhoneNumber = request.ParentPhoneNumber,
                Level = request.Level,

                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Students.Add(student);
            _context.SaveChanges();

            return Ok(student);
        }

        // =========================
        // PUT: api/Students/{id}
        // =========================
        [HttpPut("{id:int}")]
        [Authorize(Policy = "STUDENTS_EDIT")]
        public IActionResult Update(int id, UpdateStudentRequest request)
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == id);
            if (student == null)
                return NotFound("Student not found");

            // 🔁 Prevent duplicates (exclude current student)
            var duplicate = _context.Students.Any(s =>
                s.Id != id &&
                (
                    (!string.IsNullOrEmpty(request.Email) && s.Email == request.Email) ||
                    (!string.IsNullOrEmpty(request.PhoneNumber) && s.PhoneNumber == request.PhoneNumber) ||
                    (!string.IsNullOrEmpty(request.NationalId) && s.NationalId == request.NationalId)
                )
            );

            if (duplicate)
                return BadRequest("Student already exists");

            student.FullName = request.FullName;
            student.Email = request.Email;
            student.PhoneNumber = request.PhoneNumber;
            student.NationalId = request.NationalId;
            student.Gender = request.Gender;

            if (request.DateOfBirth.HasValue)
                student.DateOfBirth = request.DateOfBirth.Value;

            student.RelativeName = request.RelativeName;
            student.ParentPhoneNumber = request.ParentPhoneNumber;
            student.Level = request.Level;

            _context.SaveChanges();

            return Ok(student);
        }

        // =========================
        // DELETE: api/Students/{id}
        // =========================
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "STUDENTS_DELETE")]
        public IActionResult Archive(int id)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var student = _context.Students.FirstOrDefault(s => s.Id == id);
                if (student == null)
                    return NotFound("Student not found");

                var hasActiveEnrollments = _context.Enrollments
                    .Any(e => e.StudentId == id && e.Status != EnrollmentStatus.Completed);

                if (hasActiveEnrollments)
                    return BadRequest("Cannot archive student with active enrollments");

                var archived = new ArchivedStudent
                {
                    OriginalStudentId = student.Id,
                    FullName = student.FullName,
                    Email = student.Email,
                    PhoneNumber = student.PhoneNumber,
                    NationalId = student.NationalId,
                    Gender = student.Gender,
                    DateOfBirth = student.DateOfBirth,

                    RelativeName = student.RelativeName,
                    ParentPhoneNumber = student.ParentPhoneNumber,
                    Level = student.Level,

                    CreatedAt = student.CreatedAt,
                    ArchivedAt = DateTime.UtcNow,
                    ArchivedByUserId = 1 // TODO: from token
                };

                _context.ArchivedStudents.Add(archived);
                _context.Students.Remove(student);

                _context.SaveChanges();
                transaction.Commit();

                return Ok("Student archived successfully");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =========================
        // GET: api/Students/archived
        // =========================
        [HttpGet("archived")]
        [Authorize(Policy = "STUDENTS_VIEW")]
        public IActionResult GetArchived()
        {
            var archived = _context.ArchivedStudents
                .Select(a => new
                {
                    a.Id,
                    a.OriginalStudentId,
                    a.FullName,
                    a.Email,
                    a.PhoneNumber,
                    a.ParentPhoneNumber,
                    a.Level,
                    a.ArchivedAt
                })
                .ToList();

            return Ok(archived);
        }

        // =========================
        // POST: api/Students/archived/{id}/restore
        // =========================
        [HttpPost("archived/{id:int}/restore")]
        [Authorize(Policy = "STUDENTS_EDIT")]
        public IActionResult Restore(int id)
        {
            var archived = _context.ArchivedStudents.FirstOrDefault(a => a.Id == id);
            if (archived == null)
                return NotFound("Archived student not found");

            var student = new Student
            {
                FullName = archived.FullName,
                Email = archived.Email,
                PhoneNumber = archived.PhoneNumber,
                NationalId = archived.NationalId,
                Gender = archived.Gender,
                DateOfBirth = archived.DateOfBirth,

                RelativeName = archived.RelativeName,
                ParentPhoneNumber = archived.ParentPhoneNumber,
                Level = archived.Level,

                CreatedAt = archived.CreatedAt,
                IsActive = true
            };

            _context.Students.Add(student);
            _context.ArchivedStudents.Remove(archived);

            _context.SaveChanges();

            return Ok("Student restored successfully");
        }
    }
}
