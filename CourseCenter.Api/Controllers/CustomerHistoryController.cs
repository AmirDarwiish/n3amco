using CourseCenter.Api.CustomerHistory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/customers/{customerId:guid}/history")]
public class CustomerHistoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomerHistoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetCustomerHistory(Guid customerId)
    {
        var history = _context.CustomerHistories
            .AsNoTracking()
            .Where(entry => entry.CustomerId == customerId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new CustomerHistoryDto
            {
                Id = entry.Id,
                CustomerId = entry.CustomerId,
                EventType = entry.EventType,
                SourceEntity = entry.SourceEntity,
                SourceEntityId = entry.SourceEntityId,
                OldValue = entry.OldValue,
                NewValue = entry.NewValue,
                Notes = entry.Notes,
                CreatedAt = entry.CreatedAt,
                CreatedBy = entry.CreatedBy
            })
            .ToList();

        return Ok(history);
    }
}
