using DairySystem.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AccountsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "ACCOUNTS_VIEW")]
    public async Task<IActionResult> GetAll()
    {
        var accounts = await _context.Accounts
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.ParentId,
                x.IsActive,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(accounts));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "ACCOUNTS_VIEW")]
    public async Task<IActionResult> Get(int id)
    {
        var account = await _context.Accounts
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.ParentId,
                x.IsActive,
                x.CreatedAt,
                Children = x.Children.Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Name
                })
            })
            .FirstOrDefaultAsync();

        if (account == null)
            return NotFound(ApiResponse<string>.Fail("Account not found", "NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(account));
    }

    [HttpPost]
    [Authorize(Policy = "ACCOUNTS_CREATE")]
    public async Task<IActionResult> Create(CreateAccountDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(ApiResponse<string>.Fail("Name and Code are required", "VALIDATION_ERROR"));

        var exists = await _context.Accounts.AnyAsync(x => x.Code == dto.Code);
        if (exists)
            return BadRequest(ApiResponse<string>.Fail("Code already exists", "DUPLICATE_CODE"));

        var account = new Account
        {
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            ParentId = dto.ParentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<int>.SuccessResponse(account.Id, "Account created"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "ACCOUNTS_UPDATE")]
    public async Task<IActionResult> Update(int id, CreateAccountDto dto)
    {
        var account = await _context.Accounts.FindAsync(id);

        if (account == null)
            return NotFound(ApiResponse<string>.Fail("Account not found", "NOT_FOUND"));

        var exists = await _context.Accounts.AnyAsync(x => x.Code == dto.Code && x.Id != id);
        if (exists)
            return BadRequest(ApiResponse<string>.Fail("Code already exists", "DUPLICATE_CODE"));

        account.Code = dto.Code;
        account.Name = dto.Name;
        account.Type = dto.Type;
        account.ParentId = dto.ParentId;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "ACCOUNTS_DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await _context.Accounts.FindAsync(id);

        if (account == null)
            return NotFound(ApiResponse<string>.Fail("Account not found", "NOT_FOUND"));

        var hasLines = await _context.JournalLines.AnyAsync(x => x.AccountId == id);
        if (hasLines)
            return BadRequest(ApiResponse<string>.Fail("Account has journal entries, cannot delete", "HAS_ENTRIES"));

        account.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
    }

    [HttpGet("{id}/balance")]
    [Authorize(Policy = "ACCOUNTS_VIEW")]
    public async Task<IActionResult> Balance(int id)
    {
        var account = await _context.Accounts.FindAsync(id);

        if (account == null)
            return NotFound(ApiResponse<string>.Fail("Account not found", "NOT_FOUND"));

        var allIds = await GetAllDescendantIds(id);
        allIds.Add(id);

        var debit = await _context.JournalLines
            .Where(x => allIds.Contains(x.AccountId))
            .SumAsync(x => x.Debit);

        var credit = await _context.JournalLines
            .Where(x => allIds.Contains(x.AccountId))
            .SumAsync(x => x.Credit);

        var balance = (account.Type == AccountType.Asset || account.Type == AccountType.Expense)
            ? debit - credit
            : credit - debit;

        return Ok(ApiResponse<object>.SuccessResponse(new { accountId = id, debit, credit, balance }));
    }

    private async Task<List<int>> GetAllDescendantIds(int parentId)
    {
        var children = await _context.Accounts
            .Where(x => x.ParentId == parentId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();

        var result = new List<int>(children);
        foreach (var childId in children)
            result.AddRange(await GetAllDescendantIds(childId));

        return result;
    }
    [HttpGet("all-balances")]
    [Authorize(Policy = "ACCOUNTS_VIEW")]
    public async Task<IActionResult> AllBalances()
    {
        // جيب كل الحسابات النشطة مرة واحدة
        var accounts = await _context.Accounts
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.ParentId, x.Type })
            .ToListAsync();

        // جيب كل أرصدة الأوراق (JournalLines) مرة واحدة
        var lines = await _context.JournalLines
            .GroupBy(x => x.AccountId)
            .Select(g => new {
                AccountId = g.Key,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync();

        var lineMap = lines.ToDictionary(x => x.AccountId);

        // ابني الـ map في الميموري
        // كل حساب: اجمع أرصدة كل أبنائه recursively
        var accountMap = accounts.ToDictionary(x => x.Id);
        var childrenMap = accounts
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        decimal GetDebit(int id)
        {
            var own = lineMap.TryGetValue(id, out var l) ? l.Debit : 0;
            var childSum = childrenMap.TryGetValue(id, out var kids)
                ? kids.Sum(kid => GetDebit(kid))
                : 0;
            return own + childSum;
        }

        decimal GetCredit(int id)
        {
            var own = lineMap.TryGetValue(id, out var l) ? l.Credit : 0;
            var childSum = childrenMap.TryGetValue(id, out var kids)
                ? kids.Sum(kid => GetCredit(kid))
                : 0;
            return own + childSum;
        }

        var result = accounts.Select(a => {
            var debit = GetDebit(a.Id);
            var credit = GetCredit(a.Id);
            var balance = (a.Type == AccountType.Asset || a.Type == AccountType.Expense)
                ? debit - credit
                : credit - debit;

            return new
            {
                accountId = a.Id,
                debit,
                credit,
                balance
            };
        });

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }
}
public class CreateAccountDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public AccountType Type { get; set; }
    public int? ParentId { get; set; }
}