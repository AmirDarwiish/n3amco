using n3amco.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JournalController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJournalService _journalService;

    public JournalController(ApplicationDbContext context, IJournalService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    [HttpGet]
    [Authorize(Policy = "JOURNAL_VIEW")]
    public async Task<IActionResult> GetAll([FromQuery] JournalQuery query)
    {
        var entries = _context.JournalEntries.AsQueryable();

        if (query.Type.HasValue)
            entries = entries.Where(x => x.Type == query.Type);

        if (query.IsPosted.HasValue)
            entries = entries.Where(x => x.IsPosted == query.IsPosted);

        if (query.From.HasValue)
            entries = entries.Where(x => x.Date >= query.From);

        if (query.To.HasValue)
            entries = entries.Where(x => x.Date <= query.To);

        var totalCount = await entries.CountAsync();

        var data = await entries
            .OrderByDescending(x => x.Date)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                x.Id,
                x.Date,
                x.Description,
                x.Reference,
                x.Type,
                x.IsPosted,
                x.CreatedBy,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            totalCount,
            query.Page,
            query.PageSize,
            data
        }));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "JOURNAL_VIEW")]
    public async Task<IActionResult> Get(int id)
    {
        var entry = await _context.JournalEntries
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Date,
                x.Description,
                x.Reference,
                x.Type,
                x.IsPosted,
                x.CreatedBy,
                x.CreatedAt,
                Lines = x.Lines.Select(l => new
                {
                    l.Id,
                    l.AccountId,
                    AccountName = l.Account.Name,
                    l.Debit,
                    l.Credit,
                    l.Notes
                })
            })
            .FirstOrDefaultAsync();

        if (entry == null)
            return NotFound(ApiResponse<string>.Fail("Entry not found", "NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(entry));
    }

    [HttpPost("{id}/post")]
    [Authorize(Policy = "JOURNAL_POST")]
    public async Task<IActionResult> Post(int id)
    {
        var entry = await _context.JournalEntries.FindAsync(id);

        if (entry == null)
            return NotFound(ApiResponse<string>.Fail("Entry not found", "NOT_FOUND"));

        if (entry.IsPosted)
            return BadRequest(ApiResponse<string>.Fail("Entry already posted", "ALREADY_POSTED"));

        entry.IsPosted = true;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Entry posted"));
    }

    [HttpPost("manual")]
    [Authorize(Policy = "JOURNAL_CREATE")]
    public async Task<IActionResult> CreateManual(CreateManualJournalDto dto)
    {
        if (dto.Lines == null || dto.Lines.Count < 2)
            return BadRequest(ApiResponse<string>.Fail("Minimum 2 lines required", "VALIDATION_ERROR"));

        var totalDebit = dto.Lines.Sum(l => l.Debit);
        var totalCredit = dto.Lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
            return BadRequest(ApiResponse<string>.Fail(
                $"Entry not balanced — Debit: {totalDebit}, Credit: {totalCredit}",
                "UNBALANCED_ENTRY"));

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var entry = new JournalEntry
            {
                Date = dto.Date,
                Description = dto.Description,
                Reference = dto.Reference,
                Type = JournalEntryType.Manual,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                Lines = dto.Lines.Select(l => new JournalLine
                {
                    AccountId = l.AccountId,
                    Debit = l.Debit,
                    Credit = l.Credit,
                    Notes = l.Notes
                }).ToList()
            };

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<int>.SuccessResponse(entry.Id, "Entry created"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<string>.Fail("Internal server error", "SERVER_ERROR"));
        }
    }
}
public class JournalQuery
{
    public JournalEntryType? Type { get; set; }
    public bool? IsPosted { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CreateManualJournalDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public string Reference { get; set; }
    public List<JournalLineDto> Lines { get; set; }
}

public class JournalLineDto
{
    public int AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Notes { get; set; }
}