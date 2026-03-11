using DairySystem.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Claims;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupplierPaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SupplierPaymentsController> _logger;

    public SupplierPaymentsController(ApplicationDbContext context, ILogger<SupplierPaymentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 🥇 Create Payment
    [HttpPost]
    [Authorize(Policy = "SUPPLIER_PAYMENT_CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierPaymentCommand request)
    {
        try
        {
            if (request.Amount <= 0)
                return BadRequest(ApiResponse<string>.Fail("Amount must be greater than zero", "VALIDATION_ERROR"));

            var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
            if (supplier == null)
                return NotFound(ApiResponse<string>.Fail("Supplier not found", "NOT_FOUND"));

            // جيب الحسابات المطلوبة
            var systemAccounts = await _context.AccountSettings
        .Where(x => x.Key == "SupplierPayable" || x.Key == "Cash")
        .ToDictionaryAsync(x => x.Key, x => x.AccountId);

            if (!systemAccounts.ContainsKey("SupplierPayable") || !systemAccounts.ContainsKey("Cash"))
                return StatusCode(500, ApiResponse<string>.Fail("System accounts not configured", "CONFIG_ERROR"));

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. سجل الدفعة
                var payment = new SupplierPayment
                {
                    SupplierId = request.SupplierId,
                    Amount = request.Amount,
                    PaymentMethodId = request.PaymentMethodId,
                    Notes = request.Notes,
                    Date = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SupplierPayments.Add(payment);

                // 2. حدّث رصيد المورد
                supplier.CurrentBalance -= request.Amount;

                // 3. Ledger Entry
                var ledger = new SupplierLedger
                {
                    SupplierId = supplier.Id,
                    Amount = -request.Amount,
                    Type = SupplierTransactionType.Payment,
                    Date = DateTime.UtcNow,
                    ReferenceId = payment.Id,
                    Notes = "Supplier payment"
                };
                _context.SupplierLedgers.Add(ledger);

                // 4. القيد المحاسبي
                await _context.SaveChangesAsync(); // عشان نحتاج payment.Id

                var journal = new JournalEntry
                {
                    Date = DateTime.UtcNow,
                    Description = $"دفعة للمورد: {supplier.Name}",
                    Reference = $"PAY-{payment.Id}",
                    IsPosted = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    Lines = new List<JournalLine>
            {
                new JournalLine // مدين: ذمم الموردين
                {
                    AccountId = systemAccounts["SupplierPayable"],
                    Debit = request.Amount,
                    Credit = 0,
                    Notes = $"دفعة للمورد {supplier.Name}"
                },
                new JournalLine // دائن: الصندوق
                {
                    AccountId = systemAccounts["Cash"],
                    Debit = 0,
                    Credit = request.Amount,
                    Notes = $"دفعة للمورد {supplier.Name}"
                }
            }
                };
                _context.JournalEntries.Add(journal);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ApiResponse<int>.SuccessResponse(payment.Id, "Payment recorded"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Supplier payment failed");

                // مؤقتاً عشان نشوف المشكلة
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message + " | " + ex.InnerException?.Message, "SERVER_ERROR"));
            }
        }
        catch (Exception ex)
        {

            return StatusCode(500, ApiResponse<string>.Fail(ex.Message + " | " + ex.InnerException?.Message, "SERVER_ERROR"));
        }

    }

    // 🥈 Get All
    [HttpGet]
    [Authorize(Policy = "SUPPLIER_PAYMENT_VIEW")]
    public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
    {
        var query = _context.SupplierPayments
            .Include(x => x.Supplier)
            .AsQueryable();

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                Supplier = x.Supplier.Name,
                x.Amount,
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

    // 🥉 Get By Id
    [HttpGet("{id}")]
    [Authorize(Policy = "SUPPLIER_PAYMENT_VIEW")]
    public async Task<IActionResult> Get(int id)
    {
        var payment = await _context.SupplierPayments
            .Include(x => x.Supplier)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                Supplier = x.Supplier.Name,
                x.Amount,
                x.Date,
                x.Notes
            })
            .FirstOrDefaultAsync();

        if (payment == null)
            return NotFound(ApiResponse<string>.Fail("Payment not found", "NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(payment));
    }
}