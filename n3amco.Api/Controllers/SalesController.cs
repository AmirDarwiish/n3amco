using n3amco.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJournalService _journalService;

    public SalesController(ApplicationDbContext context, IJournalService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    [HttpGet]
    [Authorize(Policy = "SALES_READ")]
    public async Task<IActionResult> GetAll()
    {
        var sales = await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Customer)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.CustomerId,
                CustomerName = s.Customer != null ? s.Customer.Name : null,
                s.TotalAmount,
                s.TotalCost,
                s.CreatedAt,
                Items = s.Items.Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.SellingPrice,
                    i.TotalPrice,
                    i.TotalCost
                }).ToList()
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(sales, "Success"));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "SALES_READ")]
    public async Task<IActionResult> GetById(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.CustomerId,
                CustomerName = s.Customer != null ? s.Customer.Name : null,
                s.TotalAmount,
                s.TotalCost,
                s.CreatedAt,
                Items = s.Items.Select(i => new
                {
                    i.ProductId,
                    ProductName = i.Product.Name,
                    i.Quantity,
                    i.SellingPrice,
                    i.TotalPrice,
                    i.TotalCost
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (sale == null)
            return NotFound(ApiResponse<string>.Fail("Sale not found", "NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(sale, "Success"));
    }

    [HttpPost]
    [Authorize(Policy = "SALES_CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateSaleCommand request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sale = new Sale
            {
                CustomerId = request.CustomerId,
                Date = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Items = new List<SaleItem>()
            };

            decimal totalAmount = 0;
            decimal totalCost = 0;

            foreach (var item in request.Items)
            {
                var remainingQty = item.Quantity;
                decimal itemCost = 0;

                var batches = await _context.ProductBatches
                    .Where(x => x.ProductId == item.ProductId && x.RemainingQuantity > 0)
                    .OrderBy(x => x.ProductionDate)
                    .ToListAsync();

                foreach (var batch in batches)
                {
                    if (remainingQty <= 0)
                        break;

                    var taken = Math.Min(batch.RemainingQuantity, remainingQty);
                    batch.RemainingQuantity -= taken;
                    itemCost += taken * batch.CostPrice;
                    remainingQty -= taken;
                }

                if (remainingQty > 0)
                    return BadRequest(ApiResponse<string>.Fail("Not enough stock", "NO_STOCK"));

                var saleItem = new SaleItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    SellingPrice = item.Price,
                    CostPrice = itemCost / item.Quantity,
                    TotalPrice = item.Quantity * item.Price,
                    TotalCost = itemCost
                };

                sale.Items.Add(saleItem);
                totalAmount += saleItem.TotalPrice;
                totalCost += saleItem.TotalCost;
            }

            sale.TotalAmount = totalAmount;
            sale.TotalCost = totalCost;

            // بعد
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            if (request.CustomerId.HasValue && !request.IsPaid)
            {
                var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                if (customer != null)
                {
                    customer.CurrentBalance += totalAmount;
                    _context.CustomerLedgers.Add(new CustomerLedger
                    {
                        CustomerId = customer.Id,
                        Amount = totalAmount,
                        Type = CustomerTransactionType.Sale,
                        Date = DateTime.UtcNow,
                        Notes = $"فاتورة #{sale.Id}"
                    });
                    await _context.SaveChangesAsync();
                }
            }

            // ✅ القيد المحاسبي جوا نفس الـ transaction
            await _journalService.CreateSaleEntryAsync(sale, request.IsPaid);
            await transaction.CommitAsync();

            return Ok(ApiResponse<int>.SuccessResponse(sale.Id, "Sale created"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<string>.Fail(ex.Message, "SERVER_ERROR"));
        }
    }
}