using n3amco.Api;
using n3amco.Api.Common;
using n3amco.Api.Units;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace n3amco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnitsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitsController> _logger;

        public UnitsController(ApplicationDbContext context, ILogger<UnitsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========================= CREATE =========================
        [HttpPost]
        [Authorize(Policy = "UNITS_CREATE")]
        public async Task<IActionResult> Create(CreateUnitDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(ApiResponse<string>.Fail("Name is required"));

            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(ApiResponse<string>.Fail("Code is required"));

            if (await _context.Units.AnyAsync(x => x.Code == dto.Code))
                return BadRequest(ApiResponse<string>.Fail("Code already exists"));

            try
            {
                var unit = new Unit
                {
                    Name = dto.Name.Trim(),
                    Code = dto.Code.Trim().ToUpper(),
                    IsActive = true
                };

                _context.Units.Add(unit);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<int>.SuccessResponse(unit.Id, "Unit created"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create Unit Failed");
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
        }

        // ========================= GET ALL =========================
        [HttpGet]
        [Authorize(Policy = "UNITS_VIEW")]
        public async Task<IActionResult> GetAll([FromQuery] UnitQuery query)
        {
            var units = _context.Units.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                units = units.Where(x =>
                    x.Name.Contains(query.Search) ||
                    x.Code.Contains(query.Search));
            }

            if (query.IsActive.HasValue)
            {
                units = units.Where(x => x.IsActive == query.IsActive);
            }

            var total = await units.CountAsync();

            var data = await units
                .OrderByDescending(x => x.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Code,
                    x.IsActive
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

        // ========================= GET BY ID =========================
        [HttpGet("{id}")]
        [Authorize(Policy = "UNITS_VIEW")]
        public async Task<IActionResult> Get(int id)
        {
            var unit = await _context.Units.FindAsync(id);

            if (unit == null)
                return NotFound(ApiResponse<string>.Fail("Unit not found"));

            return Ok(ApiResponse<Unit>.SuccessResponse(unit));
        }

        // ========================= UPDATE =========================
        [HttpPut("{id}")]
        [Authorize(Policy = "UNITS_UPDATE")]
        public async Task<IActionResult> Update(int id, UpdateUnitDto dto)
        {
            var unit = await _context.Units.FindAsync(id);

            if (unit == null)
                return NotFound(ApiResponse<string>.Fail("Unit not found"));

            if (await _context.Units.AnyAsync(x => x.Code == dto.Code && x.Id != id))
                return BadRequest(ApiResponse<string>.Fail("Code already exists"));

            unit.Name = dto.Name.Trim();
            unit.Code = dto.Code.Trim().ToUpper();

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Updated successfully"));
        }

        // ========================= DELETE (Soft) =========================
        [HttpDelete("{id}")]
        [Authorize(Policy = "UNITS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _context.Units.FindAsync(id);

            if (unit == null)
                return NotFound(ApiResponse<string>.Fail("Unit not found"));

            unit.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Deleted successfully"));
        }

        // ========================= ACTIVE ONLY =========================
        [HttpGet("active")]
        [AllowAnonymous] // عشان تستخدمها في dropdown
        public async Task<IActionResult> GetActive()
        {
            var units = await _context.Units
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Code
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(units));
        }
    }
}
public class CreateUnitDto
{
    public string Name { get; set; }
    public string Code { get; set; }
}
public class UpdateUnitDto
{
    public string Name { get; set; }
    public string Code { get; set; }
}
public class UnitQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}