using DairySystem.Api;
using DairySystem.Api.suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[ApiController]
[Route("api/[controller]")]
[Authorize] // لازم يكون logged in
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(ApplicationDbContext context, ILogger<SuppliersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 🥇 Create
    [HttpPost]
    [Authorize(Policy = "SUPPLIERS_CREATE")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<string>.Fail("Name is required"));

        try
        {
            var supplier = new Supplier
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Address = dto.Address,
                OpeningBalance = dto.OpeningBalance,
                CurrentBalance = dto.OpeningBalance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<int>.SuccessResponse(supplier.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create Supplier Failed");
            return StatusCode(500, ApiResponse<string>.Fail("Internal server error"));
        }
    }

    // 🥈 Get All
    [HttpGet]
    [Authorize(Policy = "SUPPLIERS_VIEW")]
    public async Task<IActionResult> GetAll([FromQuery] SupplierQuery query)
    {
        var suppliers = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            suppliers = suppliers.Where(x =>
                x.Name.Contains(query.Search) ||
                x.Phone.Contains(query.Search));
        }

        if (query.IsActive.HasValue)
        {
            suppliers = suppliers.Where(x => x.IsActive == query.IsActive);
        }

        var total = await suppliers.CountAsync();

        var data = await suppliers
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Phone,
                x.CurrentBalance
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            total,
            query.Page,
            query.PageSize,
            data
        }));
    }

    // 🥉 Get By Id
    [HttpGet("{id}")]
    [Authorize(Policy = "SUPPLIERS_VIEW")]
    public async Task<IActionResult> Get(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound(ApiResponse<string>.Fail("Supplier not found"));

        return Ok(ApiResponse<Supplier>.SuccessResponse(supplier));
    }

    // ✏️ Update
    [HttpPut("{id}")]
    [Authorize(Policy = "SUPPLIERS_UPDATE")]
    public async Task<IActionResult> Update(int id, UpdateSupplierDto dto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound(ApiResponse<string>.Fail("Supplier not found"));

        supplier.Name = dto.Name;
        supplier.Phone = dto.Phone;
        supplier.Address = dto.Address;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Updated"));
    }

    // ❌ Delete
    [HttpDelete("{id}")]
    [Authorize(Policy = "SUPPLIERS_DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound(ApiResponse<string>.Fail("Supplier not found"));

        supplier.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessResponse("Deleted"));
    }

    // 💰 Balance
    [HttpGet("{id}/balance")]
    [Authorize(Policy = "SUPPLIERS_VIEW")]
    public async Task<IActionResult> GetBalance(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound(ApiResponse<string>.Fail("Supplier not found"));

        return Ok(ApiResponse<decimal>.SuccessResponse(supplier.CurrentBalance));
    }
}