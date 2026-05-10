using n3amco.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MilkCollectionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MilkCollectionsController> _logger;
    private readonly IJournalService _journalService;


    public MilkCollectionsController(ApplicationDbContext context, ILogger<MilkCollectionsController> logger, IJournalService journalService)

    {
        _context = context;
        _logger = logger;
        _journalService = journalService;

    }

    // 🥇 Create Milk Collection (🔥 CORE LOGIC)
    [HttpPost]
    [Authorize(Policy = "MILK_COLLECTION_CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateMilkCollectionCommand request)
    {
        if (request.Quantity <= 0)
            return BadRequest(ApiResponse<string>.Fail("Quantity must be greater than zero", "VALIDATION_ERROR"));

        if (request.Price <= 0)
            return BadRequest(ApiResponse<string>.Fail("Price must be greater than zero", "VALIDATION_ERROR"));

        var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
        if (supplier == null)
            return NotFound(ApiResponse<string>.Fail("Supplier not found", "NOT_FOUND"));

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
            return NotFound(ApiResponse<string>.Fail("Product not found", "NOT_FOUND"));

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 🥛 1. Create MilkCollection
            var milk = new MilkCollection
            {
                SupplierId = request.SupplierId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                PricePerUnit = request.Price,
                TotalPrice = request.Quantity * request.Price,
                Date = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.MilkCollections.Add(milk);
            await _context.SaveChangesAsync();

            // 🧊 2. Create Batch (FIFO)
           /* var batch = new ProductBatch
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                RemainingQuantity = request.Quantity,
                CostPrice = request.Price,
                ProductionDate = DateTime.UtcNow,
                SourceType = BatchSourceType.MilkCollection,
                ReferenceId = milk.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductBatches.Add(batch); */

            // 💰 3. Update Supplier Balance
            supplier.CurrentBalance += milk.TotalPrice;

            // 📒 4. Ledger Entry
            var ledger = new SupplierLedger
            {
                SupplierId = supplier.Id,
                Amount = milk.TotalPrice,
                Type = SupplierTransactionType.MilkCollection,
                Date = DateTime.UtcNow,
                ReferenceId = milk.Id,
                Notes = "Milk collection"
            };

            _context.SupplierLedgers.Add(ledger);

            await _context.SaveChangesAsync();
            await _journalService.CreateMilkCollectionEntryAsync(milk);


            await transaction.CommitAsync();

            return Ok(ApiResponse<int>.SuccessResponse(milk.Id, "Milk collection recorded"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Milk collection failed");

            return StatusCode(500, ApiResponse<string>.Fail("Internal server error", "SERVER_ERROR"));
        }
    }

    // 🥈 Get All (Pagination + Filter)
    [HttpGet]
    [Authorize(Policy = "MILK_COLLECTION_VIEW")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _context.MilkCollections
            .Include(x => x.Supplier)
            .Include(x => x.Product)
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
                Product = x.Product.Name,
                x.Quantity,
                x.PricePerUnit,
                x.TotalPrice,
                x.Date
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
    [Authorize(Policy = "MILK_COLLECTION_VIEW")]
    public async Task<IActionResult> Get(int id)
    {
        var milk = await _context.MilkCollections
            .Include(x => x.Supplier)
            .Include(x => x.Product)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                Supplier = x.Supplier.Name,
                Product = x.Product.Name,
                x.Quantity,
                x.PricePerUnit,
                x.TotalPrice,
                x.Date
            })
            .FirstOrDefaultAsync();

        if (milk == null)
            return NotFound(ApiResponse<string>.Fail("Milk collection not found", "NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(milk));
    }

    // 📊 Daily Report
    [HttpGet("daily")]
    [Authorize(Policy = "MILK_COLLECTION_VIEW")]
    public async Task<IActionResult> Daily([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;

        var data = await _context.MilkCollections
            .Where(x => x.Date.Date == targetDate.Date)
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalAmount = g.Sum(x => x.TotalPrice),
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

        return Ok(ApiResponse<object>.SuccessResponse(
            data ?? new
            {
                TotalQuantity = 0m,
                TotalAmount = 0m,
                Count = 0
            }));
    }
}