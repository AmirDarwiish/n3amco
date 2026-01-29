using CourseCenter.Api.Courses;
using CourseCenter.Api.Courses.DTOs;
using CourseCenter.Api.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CourseCenter.Api.Common;

namespace CourseCenter.Api.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: api/courses
        // =========================
        [HttpGet]
        [Authorize(Policy = "COURSES_VIEW")]
        public IActionResult GetAll([FromQuery] PagedRequest request)
        {
            if (request == null)
                request = new PagedRequest { PageNumber = 1, PageSize = 10 };

            if (request.PageNumber <= 0)
                request.PageNumber = 1;

            if (request.PageSize <= 0)
                request.PageSize = 10;

            request.PageSize = Math.Min(request.PageSize, 100);

            var query = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Classes)
                .Where(c => c.IsActive)
                .AsQueryable();

            var totalCount = query.Count();

            var dataAnon = query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    c.Description,
                    c.EstimatedDurationHours,
                    c.Level,
                    c.Language,
                    c.Tags,
                    Category = c.Category.Name,
                    ClassesCount = c.Classes.Count,
                    c.IsActive,
                    c.CreatedAt
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
        // POST: api/courses
        // =========================
        [HttpPost]
        [Authorize(Policy = "COURSES_CREATE")]
        public IActionResult Create(CreateCourseDto dto)
        {
            // 1️⃣ Validate Category
            var categoryExists = _context.Categories
                .Any(c => c.Id == dto.CategoryId && c.IsActive);

            if (!categoryExists)
                return BadRequest("Invalid category");

            // 2️⃣ Map DTO → Entity
            var course = new Course
            {
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                EstimatedDurationHours = dto.EstimatedDurationHours,
                Level = dto.Level,
                Language = dto.Language,
                Tags = dto.Tags,
                LearningOutcomes = dto.LearningOutcomes,
                Prerequisites = dto.Prerequisites,
                ThumbnailUrl = dto.ThumbnailUrl,
                CategoryId = dto.CategoryId
            };



            // 3️⃣ Save
            _context.Courses.Add(course);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Course created successfully",
                courseId = course.Id
            });
        }


        // =========================
        // PUT: api/courses/{id}
        // =========================
       /* [HttpPut("{id}")]
        public IActionResult Update(int id, Course updated)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
                return NotFound();

            course.Name = updated.Name;
            course.Code = updated.Code;
            course.Description = updated.Description;
            course.Price = updated.Price;
            course.DurationInHours = updated.DurationInHours;
            course.MaxStudents = updated.MaxStudents;
            course.StartDate = updated.StartDate;
            course.EndDate = updated.EndDate;
            course.CategoryId = updated.CategoryId;

            _context.SaveChanges();

            return Ok("Course updated");
        }
       */
        // =========================
        // PUT: api/courses/{id}/disable
        // =========================
        [HttpPut("{id}/disable")]
       /* public IActionResult Disable(int id)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
                return NotFound();

            course.IsActive = false;
            _context.SaveChanges();

            return Ok("Course disabled");
        } */
        // =========================
        // DELETE: api/courses/{id}
        // =========================
        [HttpDelete("{id}")]
        [Authorize(Policy = "COURSES_DELETE")]
        public IActionResult Delete(int id)
        {
            // 1️⃣ Check course exists
            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
                return NotFound("Course not found");

            // 2️⃣ Already deleted
            if (!course.IsActive)
                return BadRequest("Course already deleted");

            // 3️⃣ Check active enrollments (من غير Navigation)
            var hasActiveEnrollments = _context.Enrollments
     .Include(e => e.CourseClass)
     .Any(e =>
         e.CourseClass.CourseId == id &&
         e.Status != EnrollmentStatus.Completed
     );



            if (hasActiveEnrollments)
                return BadRequest("Cannot delete course with active enrollments");

            // 4️⃣ Soft delete
            course.IsActive = false;
            _context.SaveChanges();

            return Ok("Course deleted successfully");
        }
        // =========================
        // GET: api/courses/{id}
        // =========================
        [HttpGet("{id}")]
        [Authorize(Policy = "COURSES_VIEW")]
        public IActionResult GetById(int id)
        {
            var course = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Classes)
                .Where(c => c.Id == id)
               .Select(c => new
               {
                   c.Id,
                   c.Name,
                   c.Code,
                   c.Description,
                   c.Level,
                   c.EstimatedDurationHours,
                   CategoryName = c.Category.Name,
                   ClassesCount = c.Classes.Count,
                   c.IsActive,
                   c.Language,
                   c.Tags,
                   c.LearningOutcomes,
                   c.Prerequisites,
                   c.ThumbnailUrl,

               })

                .FirstOrDefault();

            if (course == null)
                return NotFound("Course not found");

            return Ok(course);
        }
        // =========================
        // PUT: api/courses/{id}
        // =========================
        [HttpPatch("{id}")]
        [Authorize(Policy = "COURSES_EDIT")]
        public IActionResult Patch(int id, [FromBody] CoursePatchRequest dto)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
                return NotFound("Course not found");

            if (dto.CategoryId.HasValue)
            {
                var categoryExists = _context.Categories
                    .Any(c => c.Id == dto.CategoryId && c.IsActive);

                if (!categoryExists)
                    return BadRequest("Invalid category");

                course.CategoryId = dto.CategoryId.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
                course.Name = dto.Name.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Code))
                course.Code = dto.Code.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Description))
                course.Description = dto.Description.Trim();
            if (dto.EstimatedDurationHours.HasValue)
                course.EstimatedDurationHours = dto.EstimatedDurationHours.Value;
            if (!string.IsNullOrWhiteSpace(dto.Language))
                course.Language = dto.Language.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Tags))
                course.Tags = dto.Tags.Trim();

            if (!string.IsNullOrWhiteSpace(dto.LearningOutcomes))
                course.LearningOutcomes = dto.LearningOutcomes.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Prerequisites))
                course.Prerequisites = dto.Prerequisites.Trim();

            if (!string.IsNullOrWhiteSpace(dto.ThumbnailUrl))
                course.ThumbnailUrl = dto.ThumbnailUrl.Trim();


            if (dto.Level.HasValue)
                course.Level = dto.Level.Value;

            _context.SaveChanges();

            return Ok(new
            {
                course.Id,
                course.Name,
                course.Code,
            });
        }

        // =========================
        // PUT: api/courses/{id}/toggle
        // =========================
        [HttpPut("{id}/toggle")]
        [Authorize(Policy = "COURSES_EDIT")]
        public IActionResult Toggle(int id)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
                return NotFound("Course not found");

            course.IsActive = !course.IsActive;
            _context.SaveChanges();

            return Ok(new
            {
                course.Id,
                course.IsActive
            });
        }


    }

}
