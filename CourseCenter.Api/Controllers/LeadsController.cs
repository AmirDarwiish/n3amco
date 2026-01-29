using ClosedXML.Excel;
using CourseCenter.Api.Common;
using CourseCenter.Api.Enrollments;
using CourseCenter.Api.Leads;
using CourseCenter.Api.Leads.DTOs;
using CourseCenter.Api.Payments;
using CourseCenter.Api.Students;
using CourseCenter.Api.Users;
using CourseCenter.Api.Users.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace CourseCenter.Api.Controllers
{
[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public LeadsController(ApplicationDbContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        // =====================
        // GET: api/leads
        // =====================
        [HttpGet]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetAll(
      [FromQuery] LeadStatus? status,
      [FromQuery] PagedRequest request)
        {
            // Safety guard
            if (request.PageNumber <= 0)
                request.PageNumber = 1;

            if (request.PageSize <= 0)
                request.PageSize = 10;

            request.PageSize = Math.Min(request.PageSize, 100);

            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .AsQueryable();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Permission-based visibility: users with the LEADS_VIEW_ALL permission see all leads
            // Use the authorization service so users authorized via role->permission in DB are respected
            var canViewAll = _authorizationService
                .AuthorizeAsync(User, "LEADS_VIEW_ALL")
                .GetAwaiter().GetResult()
                .Succeeded;

            if (!canViewAll)
            {
                query = query.Where(l => l.AssignedUserId == userId);
            }

            // Filters
            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);

            // Total count BEFORE pagination
            var totalCount = query.Count();

            // Pagination + projection
            var data = query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(l => new LeadListDto
                {
                    Id = l.Id,
                    FullName = l.FullName,
                    Phone = l.Phone,
                    Email = l.Email,
                    Status = l.Status,
                    Source = l.Source,
                    AssignedTo = l.AssignedUser != null
                        ? l.AssignedUser.FullName
                        : null,
                    CreatedAt = l.CreatedAt
                })
                .ToList();

            var response = new PagedResponse<LeadListDto>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return Ok(response);
        }


        // =====================
        // POST: api/leads
        // =====================
        [HttpPost]
        [Authorize(Policy = "LEADS_CREATE")]
        public IActionResult Create(CreateLeadDto dto)
        {
            // 1️⃣ Validation أساسية
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest("Full name is required");

            if (string.IsNullOrWhiteSpace(dto.Phone))
                return BadRequest("Phone is required");

            // 2️⃣ Optional: prevent duplicates
            var exists = _context.Leads.Any(l => l.Phone == dto.Phone);
            if (exists)
                return BadRequest("Lead with same phone already exists");

            // 3️⃣ Get current user
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // 4️⃣ Map DTO → Entity
            var lead = new Lead
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Source = dto.Source,

                Status = LeadStatus.New,          
              //  AssignedUserId = userId,          
                CreatedAt = DateTime.UtcNow    
            };

            _context.Leads.Add(lead);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Lead created successfully",
                leadId = lead.Id
            });
        }


        // =====================
        // PUT: api/leads/{id}/status
        // =====================
        [HttpPut("{id}/status")]
        [Authorize(Policy = "LEADS_EDIT")]
        public IActionResult UpdateStatus(int id, LeadStatus status)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound();

            lead.Status = status;
            _context.SaveChanges();

            return Ok("Lead status updated");
        }

        // =====================
        // POST: api/leads/{id}/convert
        // =====================
       [Authorize(Policy = "LEADS_CONVERT")]
        [HttpPost("{id}/convert")]
        public IActionResult Convert(int id, ConvertLeadDto dto)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
                if (lead == null)
                    return NotFound("Lead not found");

                if (lead.Status == LeadStatus.Converted)
                    return BadRequest("Lead already converted");
                var courseClass = _context.CourseClasses
       .Include(c => c.Course)
       .FirstOrDefault(c => c.Id == dto.CourseClassId);

                if (courseClass == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid class",
                        details = "Course class does not exist"
                    });
                }



                // 1️⃣ Create Student
                var student = new Student
                {
                    FullName = lead.FullName,
                    PhoneNumber = lead.Phone,
                    Email = lead.Email,
                    IsActive = true
                };

                _context.Students.Add(student);
                _context.SaveChanges();

                // 2️⃣ Create Enrollment
                var enrollment = new Enrollment
                {
                    StudentId = student.Id,
                    CourseClassId = courseClass.Id,
                    CoursePrice = courseClass.Price
                };


                _context.Enrollments.Add(enrollment);
                _context.SaveChanges();
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();


                // 3️⃣ Handle Payment (OPTIONAL)
                if (dto.PaidAmount.HasValue && dto.PaidAmount.Value > 0)
                {
                    if (dto.PaidAmount.Value > courseClass.Price)
                        return BadRequest("Paid amount cannot exceed course price");

                    var progressStatus =
                        dto.PaidAmount.Value == courseClass.Price
                            ? PaymentProgressStatus.FullyPaid
                            : PaymentProgressStatus.PartiallyPaid;

                    var payment = new Payment
                    {
                        EnrollmentId = enrollment.Id,
                        Amount = dto.PaidAmount.Value,
                        Status = PaymentStatus.Paid, // ✅ عملية الدفع نفسها
                        PaymentDate = DateTime.UtcNow,
                        CreatedByUserId = int.Parse(userId)
                    };

                    _context.Payments.Add(payment);

                    // ✅ ده المهم
                    enrollment.PaymentProgressStatus = progressStatus;
                }
                else
                {
                    enrollment.PaymentProgressStatus = PaymentProgressStatus.NotPaid;
                }


                // 3️⃣ Update Lead
                lead.Status = LeadStatus.Converted;
              

                _context.SaveChanges();
                transaction.Commit();

                return Ok("Lead converted successfully");
            }
            catch (DbUpdateException ex)
            {
                transaction.Rollback();

                return StatusCode(500, new
                {
                    message = "Database error while converting lead",
                    error = ex.InnerException?.Message
                });
            }
        }
       [HttpPost("{id}/notes")]
        [Authorize(Policy = "LEADS_NOTES_CREATE")]
        public IActionResult AddNote(int id, CreateLeadNoteDto dto)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var note = new LeadNote
            {
                LeadId = id,
                Note = dto.Note,
                CreatedByUserId = userId
            };

            _context.LeadNotes.Add(note);
            _context.SaveChanges();

            return Ok("Note added");
        }
        [Authorize(Policy = "View_Notes")]
        [HttpGet("{id}/notes")]
        public IActionResult GetNotes(int id)
        {
            var notes = _context.LeadNotes
                .Where(n => n.LeadId == id)
                .Include(n => n.CreatedByUser)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Note,
                    CreatedBy = n.CreatedByUser.FullName,
                    n.CreatedAt
                })
                .ToList();

            return Ok(notes);
        }
        [HttpPut("{id}/assign")]
        [Authorize(Policy = "LEADS_ASSIGN")]
        public IActionResult AssignLead(int id, AssignLeadDto dto)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            var user = _context.Users.FirstOrDefault(u => u.Id == dto.UserId && u.IsActive);
            if (user == null)
                return BadRequest("Invalid user");

            lead.AssignedUserId = dto.UserId;
            _context.SaveChanges();

            return Ok("Lead assigned successfully");
        }
        [HttpPut("{id}/follow-up")]
        [Authorize(Policy = "LEADS_EDIT")]
        public IActionResult SetFollowUp(int id, [FromBody] SetFollowUpDto dto)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            lead.FollowUpDate = dto.FollowUpDate;
            lead.FollowUpReason = dto.Reason;

            _context.SaveChanges();

            // ✅ نرجّع object مش string
            return Ok(new LeadFollowUpDto
            {
                LeadId = lead.Id,
                FollowUpDate = lead.FollowUpDate,
                FollowUpReason = lead.FollowUpReason
            });
        }
        [HttpGet("{id}/follow-up")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetFollowUp(int id)
        {
            var followUp = _context.Leads
                .Where(l => l.Id == id)
                .Select(l => new LeadFollowUpDto
                {
                    LeadId = l.Id,
                    FollowUpDate = l.FollowUpDate,
                    FollowUpReason = l.FollowUpReason
                })
                .FirstOrDefault();

            if (followUp == null)
                return NotFound("Lead not found");

            return Ok(followUp);
        }


        [HttpGet("follow-ups/today")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult TodayFollowUps()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.IsInRole("Admin");

            var today = DateTime.Today;

            var query = _context.Leads
                .Where(l =>
                    l.FollowUpDate.HasValue &&
                    l.FollowUpDate.Value.Date == today
                );

            if (!isAdmin)
                query = query.Where(l => l.AssignedUserId == userId);

            var leads = query.Select(l => new
            {
                l.Id,
                l.FullName,
                l.Phone,
                l.Status,
                l.FollowUpReason
            }).ToList();

            return Ok(leads);
        }
        [HttpGet("follow-ups/overdue")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult OverdueFollowUps()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.IsInRole("Admin");

            var today = DateTime.Today;

            var query = _context.Leads
                .Where(l =>
                    l.FollowUpDate.HasValue &&
                    l.FollowUpDate.Value.Date < today &&
                    l.Status != LeadStatus.Converted &&
                    l.Status != LeadStatus.Lost
                );

            if (!isAdmin)
                query = query.Where(l => l.AssignedUserId == userId);

            return Ok(query.Select(l => new
            {
                l.Id,
                l.FullName,
                l.Phone,
                l.FollowUpDate
            }).ToList());
        }
        // =====================
        // GET: api/leads/follow-ups/range
        // =====================
        [HttpGet("follow-ups/range")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult FollowUpsByRange(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            if (from > to)
                return BadRequest("From date cannot be after To date");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.IsInRole("Admin");

            // نخلي المقارنة على مستوى اليوم
            var fromDate = from.Date;
            var toDate = to.Date;

            var query = _context.Leads
                .Where(l =>
                    l.FollowUpDate.HasValue &&
                    l.FollowUpDate.Value.Date >= fromDate &&
                    l.FollowUpDate.Value.Date <= toDate
                );

            if (!isAdmin)
            {
                query = query.Where(l => l.AssignedUserId == userId);
            }

            var result = query
                .OrderBy(l => l.FollowUpDate)
                .Select(l => new
                {
                    l.Id,
                    l.FullName,
                    l.Phone,
                    l.Status,
                    l.FollowUpDate,
                    l.FollowUpReason
                })
                .ToList();

            return Ok(result);
        }
        [HttpGet("export")]
        [Authorize(Policy = "LEADS_EXPORT")]
        public IActionResult ExportToExcel([FromQuery] LeadStatus? status)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .AsQueryable();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Permission-based visibility: users with the LEADS_VIEW_ALL permission see all leads
            // Use authorization service so role->permission mappings (PermissionHandler) are honored
            var canViewAll = _authorizationService
                .AuthorizeAsync(User, "LEADS_VIEW_ALL")
                .GetAwaiter().GetResult()
                .Succeeded;

            if (!canViewAll)
                query = query.Where(l => l.AssignedUserId == userId);

            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);

            var leads = query
                .OrderByDescending(l => l.CreatedAt)
                .ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Leads");

            // Header
           // sheet.Cell(1, 1).Value = "ID";
            sheet.Cell(1, 1).Value = "Full Name";
            sheet.Cell(1, 2).Value = "Phone";
            sheet.Cell(1, 3).Value = "Email";
            sheet.Cell(1, 4).Value = "Source";
            sheet.Cell(1, 5).Value = "Status";
            sheet.Cell(1, 6).Value = "Assigned To";
            sheet.Cell(1, 7).Value = "Created At";

            int row = 2;
            foreach (var l in leads)
            {
             //   sheet.Cell(row, 1).Value = l.Id;
                sheet.Cell(row, 1).Value = l.FullName;
                sheet.Cell(row, 2).Value = l.Phone;
                sheet.Cell(row, 3).Value = l.Email;
                sheet.Cell(row, 4).Value = l.Source;
                sheet.Cell(row, 5).Value = l.Status.ToString();
                sheet.Cell(row, 6).Value = l.AssignedUser?.FullName;
                sheet.Cell(row, 7).Value = l.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Leads_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            );
        }
        [HttpPost("import")]
        [Authorize(Policy = "LEADS_IMPORT")]
        public IActionResult ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload an Excel file");

            var result = new ImportLeadsResultDto();

            using var stream = new MemoryStream();
            file.CopyTo(stream);

            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var rows = sheet.RangeUsed().RowsUsed().Skip(1); // skip header

            foreach (var row in rows)
            {
                result.TotalRows++;

                var fullName = row.Cell(1).GetString();
                var phone = row.Cell(2).GetString();
                var email = row.Cell(3).GetString();
                var source = row.Cell(4).GetString();

                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
                {
                    result.Skipped++;
                    result.Errors.Add($"Row {row.RowNumber()}: FullName or Phone is missing");
                    continue;
                }

                // prevent duplicates
                var exists = _context.Leads.Any(l => l.Phone == phone);
                if (exists)
                {
                    result.Skipped++;
                    continue;
                }

                var lead = new Lead
                {
                    FullName = fullName,
                    Phone = phone,
                    Email = email,
                    Source = source,
                    Status = LeadStatus.New,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Leads.Add(lead);
                result.Imported++;
            }

            _context.SaveChanges();

            return Ok(result);
        }

        public class SetFollowUpDto
        {
            public DateTime FollowUpDate { get; set; }
            public string? Reason { get; set; }
        }

        public class LeadFollowUpDto
        {
            public int LeadId { get; set; }
            public DateTime? FollowUpDate { get; set; }
            public string? FollowUpReason { get; set; }
        }
        // =====================
        // GET: api/leads/{id}
        // =====================
        [HttpGet("{id}")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetById(int id)
        {
            var lead = _context.Leads
                .Include(l => l.AssignedUser)
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    l.Id,
                    l.FullName,
                    l.Phone,
                    l.Email,
                    l.Source,
                    l.Status,
                    l.AssignedUserId,
                    AssignedTo = l.AssignedUser != null
                        ? l.AssignedUser.FullName
                        : null,
                    l.FollowUpDate,
                    l.FollowUpReason,
                    l.CreatedAt
                })
                .FirstOrDefault();

            if (lead == null)
                return NotFound("Lead not found");

            return Ok(lead);
        }
        // =====================
        // PUT: api/leads/{id}
        // =====================
        [HttpPut("{id}")]
        [Authorize(Policy = "LEADS_EDIT")]
        public IActionResult Update(int id, UpdateLeadDto dto)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            if (lead.Status == LeadStatus.Converted)
                return BadRequest("Converted lead cannot be edited");

            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest("Full name is required");

            if (string.IsNullOrWhiteSpace(dto.Phone))
                return BadRequest("Phone is required");

            // Prevent duplicate phone
            var exists = _context.Leads.Any(l =>
                l.Phone == dto.Phone && l.Id != id);

            if (exists)
                return BadRequest("Another lead with same phone exists");

            lead.FullName = dto.FullName;
            lead.Phone = dto.Phone;
            lead.Email = dto.Email;
            lead.Source = dto.Source;

            _context.SaveChanges();

            return Ok("Lead updated successfully");
        }
        // =====================
        // PUT: api/leads/notes/{noteId}
        // =====================
        [HttpPut("notes/{noteId}")]
        [Authorize(Policy = "LEADS_NOTES_EDIT")]
        public IActionResult UpdateNote(int noteId, UpdateLeadNoteDto dto)
        {
            var note = _context.LeadNotes.FirstOrDefault(n => n.Id == noteId);
            if (note == null)
                return NotFound("Note not found");

            note.Note = dto.Note;
            _context.SaveChanges();

            return Ok("Note updated successfully");
        }
        // =====================
        // DELETE: api/leads/notes/{noteId}
        // =====================
        [HttpDelete("notes/{noteId}")]
        [Authorize(Policy = "LEADS_NOTES_DELETE")]
        public IActionResult DeleteNote(int noteId)
        {
            var note = _context.LeadNotes.FirstOrDefault(n => n.Id == noteId);
            if (note == null)
                return NotFound("Note not found");

            _context.LeadNotes.Remove(note);
            _context.SaveChanges();

            return Ok("Note deleted successfully");
        }
        // =====================
        // PUT: api/leads/{id}/unassign
        // =====================
        [HttpPut("{id}/unassign")]
        [Authorize(Policy = "LEADS_ASSIGN")]
        public IActionResult Unassign(int id)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            lead.AssignedUserId = null;
            _context.SaveChanges();

            return Ok("Lead unassigned successfully");
        }
        // =====================
        // PUT: api/leads/{id}/source
        // =====================
        [HttpPut("{id}/source")]
        [Authorize(Policy = "LEADS_EDIT")]
        public IActionResult UpdateSource(int id, [FromBody] string source)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            lead.Source = source;
            _context.SaveChanges();

            return Ok("Source updated");
        }


    }
}
