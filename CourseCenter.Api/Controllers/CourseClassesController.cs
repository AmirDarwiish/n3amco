using CourseCenter.Api;
using CourseCenter.Api.Courseclasses;
using CourseCenter.Api.Courseclasses.DTO;
using CourseCenter.Api.Courses;
using CourseCenter.Api.Courses.DTOs;
using CourseCenter.Api.Enrollments;
using Microsoft.AspNetCore.Authorization;
using CourseCenter.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace CourseCenter.Api.Controllers
{
    [ApiController]
    [Route("api/courses/{courseId}/classes")]
    public class CourseClassesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CourseClassesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: api/courses/{courseId}/classes
        // =========================
        [HttpGet]
        [Authorize(Policy = "CLASSES_VIEW")]
        public IActionResult Get(int courseId, [FromQuery] PagedRequest request)
        {
            if (request == null)
                request = new PagedRequest { PageNumber = 1, PageSize = 10 };

            if (request.PageNumber <= 0)
                request.PageNumber = 1;

            if (request.PageSize <= 0)
                request.PageSize = 10;

            request.PageSize = Math.Min(request.PageSize, 100);

            var query = _context.CourseClasses
                .Where(c => c.CourseId == courseId)
                .AsQueryable();

            var totalCount = query.Count();

            var dataAnon = query
                .OrderBy(c => c.StartDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    c.Price,
                    c.InstructorName,

                    c.Status,

                    c.StartDate,
                    c.EndDate,
                    c.DaysOfWeek,
                    c.TimeFrom,
                    c.TimeTo,

                    c.MaxStudents,

                    Course = new
                    {
                        c.CourseId,
                        c.Course.Name
                    }
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
        // POST: api/courses/{courseId}/classes
        // =========================
        [HttpPost]
        [Authorize(Policy = "CLASSES_CREATE")]
        public IActionResult Create(int courseId, [FromBody] CreateCourseClassDto request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Class name is required");

            var courseExists = _context.Courses.Any(c => c.Id == courseId);
            if (!courseExists)
                return NotFound("Course not found");
            var courseClass = new CourseClass
            {
                CourseId = courseId,
                Name = request.Name.Trim(),
                Code = request.Code,
                Price = request.Price,
                InstructorName = request.InstructorName,

                StartDate = request.StartDate,
                EndDate = request.EndDate,
                DaysOfWeek = request.DaysOfWeek,
                TimeFrom = request.TimeFrom,
                TimeTo = request.TimeTo,

                MaxStudents = request.MaxStudents,
                Status = ClassStatus.Planned,
                CreatedAt = DateTime.UtcNow
            };



            _context.CourseClasses.Add(courseClass);
            _context.SaveChanges();

            return Ok(new
            {
                courseClass.Id,
                courseClass.Name,
                courseClass.Status
            });
        }


        // GET: api/classes
        [HttpGet("~/api/classes")]
        [Authorize(Policy = "CLASSES_VIEW")]
        public IActionResult GetAll([FromQuery] PagedRequest request)
        {
            if (request == null)
                request = new PagedRequest { PageNumber = 1, PageSize = 10 };

            if (request.PageNumber <= 0)
                request.PageNumber = 1;

            if (request.PageSize <= 0)
                request.PageSize = 10;

            request.PageSize = Math.Min(request.PageSize, 100);

            var query = _context.CourseClasses
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            var totalCount = query.Count();

            var dataAnon = query
                .OrderBy(c => c.StartDate)
               .Skip((request.PageNumber - 1) * request.PageSize)
               .Take(request.PageSize)
               .Select(c => new
               {
                   c.Id,
                   c.Name,
                   c.Code,
                   c.Price,
                   c.InstructorName,

                   c.Status,

                   c.StartDate,
                   c.EndDate,
                   c.DaysOfWeek,
                   c.TimeFrom,
                   c.TimeTo,

                   c.MaxStudents,

                   Course = new
                   {
                       c.CourseId,
                       c.Course.Name
                   }
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
        // DELETE: api/classes/{id}
        // =========================
        [HttpDelete("~/api/classes/{id}")]
        [Authorize(Policy = "CLASSES_DELETE")]
        public IActionResult DeleteById(int id)
        {
            // 1️⃣ Get class by Id only
            var courseClass = _context.CourseClasses
                .FirstOrDefault(c => c.Id == id && !c.IsDeleted);

            if (courseClass == null)
                return NotFound("Class not found");

            // 2️⃣ Check active enrollments
            var hasActiveEnrollments = _context.Enrollments
                .Any(e =>
                    e.CourseClassId == id &&
                    e.Status == EnrollmentStatus.Active
                );

            if (hasActiveEnrollments)
                return BadRequest("Cannot delete class with active enrollments");

            // 3️⃣ Soft delete
            courseClass.IsDeleted = true;
            courseClass.Status = ClassStatus.Closed;

            _context.SaveChanges();

            return Ok("Class deleted successfully");
        }
        // =========================
        // PUT: api/classes/{id}
        // =========================
        [HttpPut("~/api/classes/{id}")]
        [Authorize(Policy = "CLASSES_EDIT")]
        public IActionResult Update(int id, [FromBody] UpdateCourseClassDto request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            var courseClass = _context.CourseClasses
                .FirstOrDefault(c => c.Id == id && !c.IsDeleted);

            if (courseClass == null)
                return NotFound("Class not found");

            // Business rules
            if (courseClass.Status == ClassStatus.Closed)
                return BadRequest("Cannot edit a closed class");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Class name is required");

            // Update fields
            courseClass.Name = request.Name.Trim();
            courseClass.Code = request.Code;
            courseClass.Price = request.Price;
            courseClass.InstructorName = request.InstructorName;

            courseClass.StartDate = request.StartDate;
            courseClass.EndDate = request.EndDate;
            courseClass.DaysOfWeek = request.DaysOfWeek;
            courseClass.TimeFrom = request.TimeFrom;
            courseClass.TimeTo = request.TimeTo;

            courseClass.MaxStudents = request.MaxStudents;
            courseClass.UpdatedAt = DateTime.UtcNow; 

            _context.SaveChanges();

            return Ok(new
            {
                courseClass.Id,
                courseClass.Name,
                courseClass.Status
            });
        }
        // =========================
        // GET: api/classes/{id}
        // =========================
        [HttpGet("~/api/classes/{id}")]
        [Authorize(Policy = "CLASSES_VIEW")]
        public IActionResult GetById(int id)
        {
            var courseClass = _context.CourseClasses
                .Where(c => c.Id == id && !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    c.Price,
                    c.InstructorName,

                    c.Status,

                    c.StartDate,
                    c.EndDate,
                    c.DaysOfWeek,
                    c.TimeFrom,
                    c.TimeTo,

                    c.MaxStudents,

                    Course = new
                    {
                        c.CourseId,
                        c.Course.Name
                    }
                })
                .FirstOrDefault();

            if (courseClass == null)
                return NotFound("Class not found");

            return Ok(courseClass);
        }
        // =========================
        // PUT: api/classes/{id}/status
        // =========================
        [HttpPut("~/api/classes/{id}/status")]
        [Authorize(Policy = "CLASSES_CHANGE_STATUS")]
        public IActionResult ChangeStatus(int id, [FromBody] ChangeClassStatusDto request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            var courseClass = _context.CourseClasses
                .FirstOrDefault(c => c.Id == id && !c.IsDeleted);

            if (courseClass == null)
                return NotFound("Class not found");

            // Business rules
            if (courseClass.Status == ClassStatus.Closed)
                return BadRequest("Closed class cannot change status");

            if (courseClass.Status == request.Status)
                return BadRequest("Class already in this status");

            // Allowed transitions
            if (courseClass.Status == ClassStatus.Planned && request.Status != ClassStatus.Open)
                return BadRequest("Planned class can only be opened");

            if (courseClass.Status == ClassStatus.Open && request.Status != ClassStatus.Closed)
                return BadRequest("Open class can only be closed");

            courseClass.Status = request.Status;
            _context.SaveChanges();

            return Ok(new
            {
                courseClass.Id,
                OldStatus = courseClass.Status,
                NewStatus = request.Status
            });
        }


    }
}
