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
using CourseCenter.Api.Leads.DTOs;
using System.Text.RegularExpressions;


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

            // Add derived CRM fields for single lead case as well (used in GetById)

            // Compute derived CRM fields from notes
            var leadIds = data.Select(d => d.Id).ToList();
            var notes = _context.LeadNotes
                .Where(n => leadIds.Contains(n.LeadId))
                .Select(n => new { n.LeadId, n.CreatedAt, n.InteractionType })
                .ToList()
                .GroupBy(n => n.LeadId)
                .ToDictionary(g => g.Key, g => new
                {
                    LastDate = g.Max(x => x.CreatedAt),
                    LastType = g.OrderByDescending(x => x.CreatedAt).First().InteractionType,
                    HasComplaint = g.Any(x => x.InteractionType == LeadInteractionType.Complaint)
                });

            foreach (var item in data)
            {
                if (notes.TryGetValue(item.Id, out var n))
                {
                    item.LastInteractionDate = n.LastDate;
                    item.LastInteractionType = n.LastType.ToString();
                    item.HasComplaint = n.HasComplaint;
                }
            }

            var response = new PagedResponse<LeadListDto>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return Ok(response);
        }

        // GET: api/leads/{id}/details
        [HttpGet("{id}/details")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetDetails(
            int id,
            [FromQuery] int timelinePage = 1,
            [FromQuery] int timelineSize = 50)
        {
            try
            {
                // =============================
                // 1️⃣ Lead basic info (NO tracking)
                // =============================
                var lead = _context.Leads
                    .AsNoTracking() // 🔥 مهم جدًا
                    .Where(l => l.Id == id)
                    .Select(l => new
                    {
                        l.Id,
                        l.FullName,
                        l.Phone,
                        l.Email,
                        l.Source,
                        l.Status,
                        l.LostReason,
                        l.CreatedAt,
                        AssignedUser = l.AssignedUser == null
                            ? null
                            : new AssignedUserDto
                            {
                                Id = l.AssignedUser.Id,
                                FullName = l.AssignedUser.FullName
                            }
                    })
                    .FirstOrDefault();

                if (lead == null)
                {
                    return NotFound(new
                    {
                        message = "Lead not found"
                    });
                }

                // 🔥 Guard حاسم: ممنوع Status = 0
                if ((int)lead.Status == 0)
                {
                    return Problem(
                        title: "Invalid lead status",
                        detail: "This lead has an invalid status (0). Please refresh or contact support.",
                        statusCode: StatusCodes.Status409Conflict
                    );
                }

                // =============================
                // 2️⃣ Build base DTO
                // =============================
                var dto = new LeadDetailsDto
                {
                    LeadInfo = new LeadInfoDto
                    {
                        Id = lead.Id,
                        Name = lead.FullName,
                        Phone = lead.Phone,
                        Email = lead.Email,
                        Source = lead.Source,
                        AssignedUser = lead.AssignedUser,
                        LostReason = lead.Status == LeadStatus.Lost
                        ? lead.LostReason
        : null
                    }
,
                    CurrentStage = new StageDto
                    {
                        Id = (int)lead.Status,
                        Name = lead.Status.ToString(),
                        Order = (int)lead.Status
                    }
                };

                // =============================
                // 3️⃣ All stages (NO zero)
                // =============================
                dto.AllStages = Enum.GetValues(typeof(LeadStatus))
                    .Cast<LeadStatus>()
                    .Where(s => (int)s > 0) // 🔒 امنع stage 0
                    .Select(s => new StageDto
                    {
                        Id = (int)s,
                        Name = s.ToString(),
                        Order = (int)s
                    })
                    .OrderBy(s => s.Order)
                    .ToList();

                // =============================
                // 4️⃣ Stage history
                // =============================
                var persistentHistory = _context.LeadStageHistory
                    .AsNoTracking()
                    .Where(h => h.LeadId == id)
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new StageHistoryDto
                    {
                        FromStage = h.FromStage != null ? h.FromStage.Name : null,
                        ToStage = h.ToStage != null ? h.ToStage.Name : null,
                        ChangedAt = h.ChangedAt,
                        ChangedBy = h.ChangedByUserId,
                        ChangedByName = h.ChangedByUser.FullName
                    })
                    .ToList();

                if (persistentHistory.Any())
                {
                    dto.StageHistory = persistentHistory;
                }
                else
                {
                    dto.StageHistory = _context.LeadNotes
                        .AsNoTracking()
                        .Where(n => n.LeadId == id &&
                                    EF.Functions.Like(n.Note, "%Stage changed%"))
                        .OrderByDescending(n => n.CreatedAt)
                        .Select(n => new StageHistoryDto
                        {
                            FromStage = null,
                            ToStage = n.Note,
                            ChangedAt = n.CreatedAt,
                            ChangedBy = n.CreatedByUserId,
                            ChangedByName = n.CreatedByUser.FullName
                        })
                        .ToList();
                }

                // =============================
                // 5️⃣ Activity Timeline (EF-safe)
                // =============================
                var notesQ = _context.LeadNotes
                    .AsNoTracking()
                    .Where(n => n.LeadId == id)
                    .Select(n => new
                    {
                        Type = "Note",
                        Description = n.Note,
                        CreatedAt = n.CreatedAt,
                        CreatedBy = n.CreatedByUserId,
                        CreatedByName = n.CreatedByUser.FullName,
                        InteractionType = (LeadInteractionType?)n.InteractionType
                    });

                var callsQ = _context.LeadCalls
                    .AsNoTracking()
                    .Where(c => c.LeadId == id)
                    .Select(c => new
                    {
                        Type = "Call",
                        Description = c.Notes,
                        CreatedAt = c.CreatedAt,
                        CreatedBy = c.CreatedByUserId,
                        CreatedByName = c.CreatedByUser.FullName,
                        InteractionType = (LeadInteractionType?)null
                    });

                var messagesQ = _context.LeadMessages
                    .AsNoTracking()
                    .Where(m => m.LeadId == id)
                    .Select(m => new
                    {
                        Type = "Message",
                        Description = m.MessagePreview,
                        CreatedAt = m.CreatedAt,
                        CreatedBy = m.CreatedByUserId,
                        CreatedByName = m.CreatedByUser.FullName,
                        InteractionType = (LeadInteractionType?)null
                    });

                var tasksQ = _context.LeadTasks
                    .AsNoTracking()
                    .Where(t => t.LeadId == id)
                    .Select(t => new
                    {
                        Type = "Task",
                        Description = t.Title,
                        CreatedAt = t.CreatedAt,
                        CreatedBy = t.CreatedByUserId,
                        CreatedByName = t.CreatedByUser.FullName,
                        InteractionType = (LeadInteractionType?)null
                    });

                var timelineRaw = notesQ
                    .Concat(callsQ)
                    .Concat(messagesQ)
                    .Concat(tasksQ)
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((timelinePage - 1) * timelineSize)
                    .Take(timelineSize)
                    .AsEnumerable();

                dto.ActivityTimeline = timelineRaw
                    .Select(x => new ActivityDto
                    {
                        Type = x.Type,
                        Description = x.Description,
                        CreatedAt = x.CreatedAt,
                        CreatedBy = x.CreatedBy,
                        CreatedByName = x.CreatedByName,
                        InteractionType = x.InteractionType?.ToString()
                    })
                    .ToList();

                // =============================
                // 6️⃣ Metrics
                // =============================
                dto.Metrics = new MetricsDto
                {
                    DaysInPipeline = (DateTime.UtcNow - lead.CreatedAt).TotalDays,
                    LastActivityAt = _context.LeadNotes
                        .AsNoTracking()
                        .Where(n => n.LeadId == id)
                        .OrderByDescending(n => n.CreatedAt)
                        .Select(n => (DateTime?)n.CreatedAt)
                        .FirstOrDefault()
                };

                return Ok(dto);
            }
            catch (DbUpdateException)
            {
                // Database / constraint issues
                return Problem(
                    title: "Database error",
                    detail: "A database error occurred while loading lead details.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
            catch (Exception ex)
            {
                // أي حاجة غير متوقعة
                return Problem(
                    title: "Unexpected error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }


        // GET: api/leads/pipeline
        [HttpGet("pipeline")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetPipeline(
     [FromQuery] LeadStatus? stage,
     [FromQuery] int? assignedUserId,
     [FromQuery] int? recentActivityDays)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var canViewAll = _authorizationService
                    .AuthorizeAsync(User, "LEADS_VIEW_ALL")
                    .GetAwaiter()
                    .GetResult()
                    .Succeeded;

                // =========================
                // 0️⃣ Defensive check (TEMP – for debugging)
                // =========================
                if (_context.Leads.AsNoTracking().Any(l => l.Status == 0))
                {
                    return Problem(
                        title: "Invalid lead data detected",
                        detail: "Some leads have invalid status (0). Please contact support.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }

                // =========================
                // 1️⃣ Base query (NO tracking)
                // =========================
                var query = _context.Leads
                    .AsNoTracking()                 // 🔥 يمنع EF tracking نهائي
                    .Include(l => l.AssignedUser)
                    .Where(l => l.Status != 0)      // 🔒 guard
                    .AsQueryable();

                if (!canViewAll)
                    query = query.Where(l => l.AssignedUserId == userId);

                if (stage.HasValue)
                    query = query.Where(l => l.Status == stage.Value);

                if (assignedUserId.HasValue)
                    query = query.Where(l => l.AssignedUserId == assignedUserId.Value);

                if (recentActivityDays.HasValue)
                {
                    var since = DateTime.UtcNow.AddDays(-recentActivityDays.Value);
                    var activeLeadIds = _context.LeadNotes
                        .AsNoTracking()
                        .Where(n => n.CreatedAt >= since)
                        .Select(n => n.LeadId)
                        .Distinct();

                    query = query.Where(l => activeLeadIds.Contains(l.Id));
                }

                // =========================
                // 2️⃣ Projection
                // =========================
                var leads = query
                    .Select(l => new
                    {
                        l.Id,
                        l.FullName,
                        l.Status,
                        AssignedUser = l.AssignedUser == null
                            ? null
                            : new AssignedUserDto
                            {
                                Id = l.AssignedUser.Id,
                                FullName = l.AssignedUser.FullName
                            }
                    })
                    .ToList();

                // =========================
                // 3️⃣ Last activity lookup
                // =========================
                var leadIds = leads.Select(l => l.Id).ToList();

                var lastActivities = _context.LeadNotes
                    .AsNoTracking()
                    .Where(n => leadIds.Contains(n.LeadId))
                    .GroupBy(n => n.LeadId)
                    .Select(g => new
                    {
                        LeadId = g.Key,
                        LastAt = g.Max(x => x.CreatedAt),
                        LastType = g.OrderByDescending(x => x.CreatedAt)
                                    .First().InteractionType
                    })
                    .ToDictionary(x => x.LeadId);

                // =========================
                // 4️⃣ Grouping
                // =========================
                var grouped = leads
                    .GroupBy(l => l.Status)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // =========================
                // 5️⃣ Build pipeline (ALL stages)
                // =========================
                var pipeline = Enum.GetValues(typeof(LeadStatus))
                    .Cast<LeadStatus>()
                    .Where(s => (int)s > 0)
                    .OrderBy(s => (int)s)
                    .Select(s => new PipelineStageDto
                    {
                        StageId = (int)s,
                        StageName = s.ToString(),
                        TotalLeads = grouped.ContainsKey(s) ? grouped[s].Count : 0,
                        Leads = grouped.ContainsKey(s)
                            ? grouped[s].Select(l => new PipelineLeadDto
                            {
                                Id = l.Id,
                                Name = l.FullName,
                                AssignedUser = l.AssignedUser,
                                LastActivityAt = lastActivities.ContainsKey(l.Id)
                                    ? lastActivities[l.Id].LastAt
                                    : null,
                                LastActivityType = lastActivities.ContainsKey(l.Id)
                                    ? lastActivities[l.Id].LastType.ToString()
                                    : null
                            }).ToList()
                            : new List<PipelineLeadDto>()
                    })
                    .ToList();

                return Ok(pipeline);
            }
            catch (Exception ex)
            {
                // أي حاجة غير متوقعة
                return Problem(
                    title: "Failed to load leads pipeline",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
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
        public IActionResult UpdateStatus(int id, [FromBody] UpdateLeadStatusDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Invalid request body");

                if ((int)dto.Status == 0)
                    return BadRequest("Invalid lead status");

                var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
                if (lead == null)
                    return NotFound("Lead not found");

                var previousStatus = lead.Status;

                // لو Lost لازم يكون في سبب
             /*   if (dto.Status == LeadStatus.Lost &&
                    string.IsNullOrWhiteSpace(dto.Reason))
                {
                    return BadRequest(new
                    {
                        message = "Lost reason is required when status is Lost"
                    });
                }
             */
                // تحديث الحالة
                lead.Status = dto.Status;

                // التعامل مع LostReason
                if (dto.Status == LeadStatus.Lost)
                {
                    lead.LostReason = dto.Reason;
                }
                else
                {
                    // لو خرج من Lost نمسح السبب
                    lead.LostReason = null;
                }

                _context.SaveChanges();

                return Ok(new
                {
                    message = "Lead status updated",
                    status = dto.Status.ToString(),
                    lostReason = lead.LostReason
                });
            }
            catch (DbUpdateException)
            {
                return Problem(
                    title: "Database error",
                    detail: "Invalid lead status update detected.",
                    statusCode: StatusCodes.Status409Conflict
                );
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Unexpected error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
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
                InteractionType = dto.InteractionType ?? LeadInteractionType.GeneralNote,
                CreatedByUserId = userId
            };

            _context.LeadNotes.Add(note);
            _context.SaveChanges();

            // Process mentions (side-effect): detect @username patterns and create LeadMention entries
            try
            {
                var matches = Regex.Matches(dto.Note ?? string.Empty, @"@([A-Za-z0-9_\.\-]+)");
                var handles = matches.Select(m => m.Groups[1].Value.Trim())
                    .Where(h => !string.IsNullOrEmpty(h))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (handles.Any())
                {
                    // fetch candidate users (active)
                    var candidates = _context.Users
                        .Where(u => u.IsActive)
                        .ToList();

                    var leadEntity = _context.Leads.FirstOrDefault(l => l.Id == id);

                    var mentionsToAdd = new List<LeadMention>();

                    foreach (var handle in handles)
                    {
                        // Resolve by email local-part or full name (best-effort)
                        var matched = candidates.FirstOrDefault(u =>
                            string.Equals(u.Email.Split('@')[0], handle, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(u.FullName.Replace(" ", ""), handle, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(u.FullName, handle, StringComparison.OrdinalIgnoreCase)
                        );

                        if (matched == null)
                            continue; // do not leak info

                        // Check if matched user can view the lead (permission-aware)
                        var roles = _context.UserRoles
                            .Where(ur => ur.UserId == matched.Id)
                            .Include(ur => ur.Role)
                            .Select(ur => ur.Role.Name)
                            .ToList();

                        var hasViewAll = _context.RolePermissions
                            .Any(rp => roles.Contains(rp.Role) && rp.Permission.Code == "LEADS_VIEW_ALL");

                        var canView = false;
                        if (hasViewAll)
                            canView = true;
                        else if (leadEntity != null && leadEntity.AssignedUserId.HasValue && leadEntity.AssignedUserId.Value == matched.Id)
                            canView = true;

                        if (!canView)
                            continue;

                        // avoid duplicate mentions for same note and user
                        var existsMention = _context.LeadMentions.Any(m => m.LeadNoteId == note.Id && m.MentionedUserId == matched.Id);
                        if (existsMention)
                            continue;

                        mentionsToAdd.Add(new LeadMention
                        {
                            LeadNoteId = note.Id,
                            MentionedUserId = matched.Id,
                            MentionedByUserId = userId,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        });
                    }
                    if (mentionsToAdd.Any())
                    {
                        _context.LeadMentions.AddRange(mentionsToAdd);
                        _context.SaveChanges();
                    }
                }
            }
            catch
            {
                // ignore mention processing errors to avoid impacting primary flow
            }

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
                    InteractionType = n.InteractionType.ToString(),
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

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // 1️⃣ Update current Follow-Up (زي ما هو)
            lead.FollowUpDate = dto.FollowUpDate.Date;
            lead.FollowUpReason = dto.Reason;

            // 2️⃣ Log (History)
            _context.LeadFollowUpLogs.Add(new LeadFollowUpLog
            {
                LeadId = id,
                FollowUpDate = dto.FollowUpDate.Date,
                Reason = dto.Reason ?? "Manual follow-up",
                Source = FollowUpSource.Manual,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            _context.SaveChanges();

            // ❗ Response زي ما هو
            return Ok(new LeadFollowUpDto
            {
                LeadId = lead.Id,
                FollowUpDate = lead.FollowUpDate,
                FollowUpReason = lead.FollowUpReason
            });
        }


        [HttpGet("follow-ups")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetFollowUps(
            [FromQuery] bool? today,
            [FromQuery] bool? overdue,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var canViewAll = _authorizationService
                .AuthorizeAsync(User, "LEADS_VIEW_ALL")
                .GetAwaiter()
                .GetResult()
                .Succeeded;

            var todayDate = DateTime.UtcNow.Date;

            var query = _context.Leads
                .AsNoTracking()
                .Where(l => l.FollowUpDate.HasValue);

            // 🔹 Today
            if (today == true)
            {
                query = query.Where(l => l.FollowUpDate!.Value.Date == todayDate);
            }

            // 🔹 Overdue
            if (overdue == true)
            {
                query = query.Where(l =>
                    l.FollowUpDate!.Value.Date < todayDate &&
                    l.Status != LeadStatus.Converted &&
                    l.Status != LeadStatus.Lost
                );
            }

            // 🔹 Range
            if (from.HasValue)
            {
                var fromDate = from.Value.ToUniversalTime().Date;
                query = query.Where(l => l.FollowUpDate!.Value.Date >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.ToUniversalTime().Date;
                query = query.Where(l => l.FollowUpDate!.Value.Date <= toDate);
            }

            // 🔹 Permissions
            if (!canViewAll)
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
            if (dto.InteractionType.HasValue)
            {
                note.InteractionType = dto.InteractionType.Value;
            }
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
        [HttpPost("{id}/tasks")]
        [Authorize(Policy = "LEADS_TASKS_CREATE")]
        public IActionResult AddTask(int id, CreateLeadTaskDto dto)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Task title is required");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // 1️⃣ Create Task (زي ما هو)
            var task = new LeadTask
            {
                LeadId = id,
                Title = dto.Title,
                DueDate = dto.DueDate?.Date,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.LeadTasks.Add(task);

            // 2️⃣ Follow-Up Logic (جديد – Side Effect)
            if (dto.DueDate.HasValue)
            {
                var followUpDate = dto.DueDate.Value.Date;

                // 🔹 Log دايمًا (History)
                _context.LeadFollowUpLogs.Add(new LeadFollowUpLog
                {
                    LeadId = id,
                    FollowUpDate = followUpDate,
                    Reason = $"Task: {dto.Title}",
                    Source = FollowUpSource.Task,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                // 🔹 Update current Follow-Up (لو أقرب)
                if (!lead.FollowUpDate.HasValue || followUpDate < lead.FollowUpDate.Value)
                {
                    lead.FollowUpDate = followUpDate;
                    lead.FollowUpReason = $"Task: {dto.Title}";
                }
            }

            _context.SaveChanges();

            // ❗ Response زي ما هو
            return Ok("Task added");
        }


        [HttpPut("{id}/archive")]
        [Authorize(Policy = "LEADS_DELETE")] // أو LEADS_ARCHIVE
        public IActionResult Archive(int id)
        {
            var lead = _context.Leads.FirstOrDefault(l => l.Id == id);
            if (lead == null)
                return NotFound("Lead not found");

            if (lead.IsArchived)
                return BadRequest("Lead already archived");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            lead.IsArchived = true;
            lead.ArchivedAt = DateTime.UtcNow;
            lead.ArchivedByUserId = userId;

            _context.SaveChanges();

            return Ok("Lead archived successfully");
        }
        [HttpPut("{id}/restore")]
        [Authorize(Policy = "LEADS_DELETE")]
        public IActionResult Restore(int id)
        {
            var lead = _context.Leads
                .IgnoreQueryFilters() // 🔥 مهم
                .FirstOrDefault(l => l.Id == id);

            if (lead == null)
                return NotFound("Lead not found");

            if (!lead.IsArchived)
                return BadRequest("Lead is not archived");

            lead.IsArchived = false;
            lead.ArchivedAt = null;
            lead.ArchivedByUserId = null;

            _context.SaveChanges();

            return Ok("Lead restored successfully");
        }
        [HttpGet("archived")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetArchived()
        {
            var leads = _context.Leads
                .IgnoreQueryFilters()
                .Where(l => l.IsArchived)
                .Select(l => new
                {
                    l.Id,
                    l.FullName,
                    l.Phone,
                    l.Status,
                    l.ArchivedAt
                })
                .ToList();

            return Ok(leads);
        }
        [HttpGet("{id}/follow-up-history")]
        [Authorize(Policy = "LEADS_VIEW")]
        public IActionResult GetFollowUpHistory(int id)
        {
            var history = _context.LeadFollowUpLogs
                .AsNoTracking()
                .Where(x => x.LeadId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.FollowUpDate,
                    x.Reason,
                    Source = x.Source.ToString(),
                    x.CreatedAt
                })
                .ToList();

            return Ok(history);
        }


    }
}
