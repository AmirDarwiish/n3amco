using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "DASHBOARD_VIEW")]
    public async Task<IActionResult> Get([FromQuery] DashboardQuery query)
    {
        try
        {
            var data = await _service.GetDashboard(query);

            return Ok(ApiResponse<object>.SuccessResponse(data));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}