using n3amco.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomersController> _logger;
    private readonly IJournalService _journalService;


    public CustomersController(
          ApplicationDbContext context,
          ILogger<CustomersController> logger,
          IJournalService journalService)
    {
        _context = context;
        _logger = logger;
        _journalService = journalService;
    }
    // 🥇 Create
    [HttpPost]
    [Authorize(Policy = "CUSTOMERS_CREATE")]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<string>.Fail("Name is required", "VALIDATION_ERROR"));

        var customer = new Customer
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Address = dto.Address,
            CreatedAt = DateTime.UtcNow,
            CurrentBalance = dto.OpeningBalance  // ← غيرها من 0
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // ✅ قيد الرصيد الافتتاحي
        if (dto.OpeningBalance > 0)
        {
            var receivableAccount = await _context.AccountSettings
                .Where(x => x.Key == "CustomerReceivable")
                .Select(x => x.AccountId)
                .FirstOrDefaultAsync();

            var entry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                Description = $"رصيد افتتاحي - عميل: {customer.Name}",
                Reference = $"CUST-OPEN-{customer.Id}",
                Type = JournalEntryType.Manual,
                IsPosted = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                Lines = new List<JournalLine>
            {
                new JournalLine
                {
                    AccountId = receivableAccount, // ذمم العملاء
                    Debit = dto.OpeningBalance,
                    Credit = 0,
                    Notes = "رصيد افتتاحي عميل"
                },
                new JournalLine
                {
                    AccountId = 12, // الأرباح المحتجزة
                    Debit = 0,
                    Credit = dto.OpeningBalance,
                    Notes = "مقابل رصيد افتتاحي عميل"
                }
            }
            };

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        return Ok(ApiResponse<int>.SuccessResponse(customer.Id, "Customer created"));
    }

    // 🥈 Get All (🔥 Advanced Query)
    [HttpGet]
    [Authorize(Policy = "CUSTOMERS_VIEW")]
    public async Task<IActionResult> GetAll([FromQuery] CustomerQuery query)
    {
        var customers = _context.Customers.AsQueryable();

        // 🔍 Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            customers = customers.Where(x =>
                x.Name.Contains(query.Search) ||
                x.Phone.Contains(query.Search));
        }

        // 💰 Balance Filter
        if (query.MinBalance.HasValue)
            customers = customers.Where(x => x.CurrentBalance >= query.MinBalance);

        if (query.MaxBalance.HasValue)
            customers = customers.Where(x => x.CurrentBalance <= query.MaxBalance);

        // 🔄 Active
        if (query.IsActive.HasValue)
            customers = customers.Where(x => x.IsActive == query.IsActive);

        // 🔃 Sorting
        customers = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDir == "asc"
                ? customers.OrderBy(x => x.Name)
                : customers.OrderByDescending(x => x.Name),

            "balance" => query.SortDir == "asc"
                ? customers.OrderBy(x => x.CurrentBalance)
                : customers.OrderByDescending(x => x.CurrentBalance),

            _ => query.SortDir == "asc"
                ? customers.OrderBy(x => x.CreatedAt)
                : customers.OrderByDescending(x => x.CreatedAt)
        };

        var totalCount = await customers.CountAsync();

        var data = await customers
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Phone,
                x.CurrentBalance,
                x.IsActive,
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

    // 🥉 Get By Id
    [HttpGet("{id}")]
    [Authorize(Policy = "CUSTOMERS_VIEW")]
    public async Task<IActionResult> Get(int id)
    {
        var customer = await _context.Customers
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Phone,
                x.Address,
                x.CurrentBalance,
                x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound(ApiResponse<string>.Fail("Customer not found", "NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(customer));
    }

    // ✏️ Update
    [HttpPut("{id}")]
    [Authorize(Policy = "CUSTOMERS_UPDATE")]
    public async Task<IActionResult> Update(int id, CreateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound(ApiResponse<string>.Fail("Customer not found", "NOT_FOUND"));

        customer.Name = dto.Name;
        customer.Phone = dto.Phone;
        customer.Address = dto.Address;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
    }

    // ❌ Soft Delete
    [HttpDelete("{id}")]
    [Authorize(Policy = "CUSTOMERS_DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound(ApiResponse<string>.Fail("Customer not found", "NOT_FOUND"));

        customer.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
    }

    // 🔄 Restore
    [HttpPut("{id}/restore")]
    [Authorize(Policy = "CUSTOMERS_UPDATE")]
    public async Task<IActionResult> Restore(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound(ApiResponse<string>.Fail("Customer not found", "NOT_FOUND"));

        customer.IsActive = true;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Restored"));
    }

    // 💰 Balance
    [HttpGet("{id}/balance")]
    [Authorize(Policy = "CUSTOMERS_VIEW")]
    public async Task<IActionResult> Balance(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound(ApiResponse<string>.Fail("Customer not found", "NOT_FOUND"));

        return Ok(ApiResponse<decimal>.SuccessResponse(customer.CurrentBalance));
    }

    // 📒 Statement (🔥 Paginated)
    [HttpGet("{id}/statement")]
    [Authorize(Policy = "CUSTOMERS_VIEW")]
    public async Task<IActionResult> Statement(int id, int page = 1, int pageSize = 10)
    {
        var query = _context.CustomerLedgers
            .Where(x => x.CustomerId == id)
            .OrderByDescending(x => x.Date);

        var total = await query.CountAsync();

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Amount,
                x.Type,
                x.Date,
                x.Notes
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            total,
            page,
            pageSize,
            data
        }));
    }

    // 💸 Payment
    [HttpPost("payment")]
    [Authorize(Policy = "CUSTOMER_PAYMENT_CREATE")]
    public async Task<IActionResult> Payment(CustomerPaymentDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(ApiResponse<string>.Fail("Invalid amount", "VALIDATION_ERROR"));

        var customer = await _context.Customers.FindAsync(dto.CustomerId);

        if (customer == null)
            return NotFound(ApiResponse<string>.Fail("Customer not found", "NOT_FOUND"));

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var payment = new CustomerPayment
            {
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                Date = DateTime.UtcNow,
                Notes = dto.Notes
            };

            customer.CurrentBalance -= dto.Amount;

            _context.CustomerPayments.Add(payment);

            _context.CustomerLedgers.Add(new CustomerLedger
            {
                CustomerId = customer.Id,
                Amount = -dto.Amount,
                Type = CustomerTransactionType.Payment,
                Date = DateTime.UtcNow,
                Notes = "Payment"
            });

            // ── القيد المحاسبي جوا نفس الـ transaction ──
            var cashAccount = await _context.AccountSettings
                .Where(x => x.Key == "Cash")
                .Select(x => x.AccountId)
                .FirstOrDefaultAsync();

            var receivableAccount = await _context.AccountSettings
                .Where(x => x.Key == "CustomerReceivable")
                .Select(x => x.AccountId)
                .FirstOrDefaultAsync();

            var entry = new JournalEntry
            {
                Date = DateTime.UtcNow,
                Description = $"قيد تحصيل — عميل #{dto.CustomerId}",
                Reference = $"PAY-{dto.CustomerId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Type = JournalEntryType.Payment,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                Lines = new List<JournalLine>
            {
                new JournalLine
                {
                    AccountId = cashAccount,
                    Debit = dto.Amount,
                    Credit = 0,
                    Notes = $"عميل #{dto.CustomerId}"
                },
                new JournalLine
                {
                    AccountId = receivableAccount,
                    Debit = 0,
                    Credit = dto.Amount,
                    Notes = $"PAY-{dto.CustomerId}"
                }
            }
            };

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Payment recorded"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Customer payment failed");
            return StatusCode(500, ApiResponse<string>.Fail("Internal server error", "SERVER_ERROR"));
        }
    }
}