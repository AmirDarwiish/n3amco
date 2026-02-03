using CourseCenter.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/reports/user-activity")]
public class UserActivityReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserActivityReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===============================
    // 1️⃣ Get all actions for a user in a day
    // ===============================
    [HttpGet("actions")]
    public IActionResult GetUserActions(
        int userId,
        DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        var actions = _context.UserActivityLogs
            .Where(x =>
                x.UserId == userId &&
                x.CreatedAt >= start &&
                x.CreatedAt < end &&
                x.ActivityType == "Action")
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                Time = x.CreatedAt,
                Entity = x.EntityName,
                Action = x.ActionName
            })
            .ToList();

        return Ok(actions);
    }

    // ===============================
    // 2️⃣ Daily summary (actions count + active hours)
    // ===============================
    [HttpGet("daily-summary")]
    public IActionResult GetDailySummary(
        int userId,
        DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        var logs = _context.UserActivityLogs
            .Where(x =>
                x.UserId == userId &&
                x.CreatedAt >= start &&
                x.CreatedAt < end)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        double activeMinutes = 0;

        for (int i = 0; i < logs.Count - 1; i++)
        {
            var diff = (logs[i + 1].CreatedAt - logs[i].CreatedAt)
                .TotalMinutes;

            // Rule: لو أقل من 15 دقيقة = Active
            if (diff <= 15)
                activeMinutes += diff;
        }

        var actionsCount = logs
            .Count(x => x.ActivityType == "Action");

        return Ok(new
        {
            UserId = userId,
            Date = date.Date,
            ActionsCount = actionsCount,
            ActiveHours = Math.Round(activeMinutes / 60, 2)
        });
    }
}
