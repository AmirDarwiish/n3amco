using CourseCenter.Api.Users.Attributes;
using CourseCenter.Api.Users.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CourseCenter.Api.Users.Filters
{
    public class UserActivityLogFilter : IAsyncActionFilter
    {
        private readonly IUserActivityLogger _logger;

        public UserActivityLogFilter(IUserActivityLogger logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var executedContext = await next();

            // لو حصل Exception → منسجلش
            if (executedContext.Exception != null)
                return;

            // Skip Attribute
            if (context.ActionDescriptor.EndpointMetadata
                .Any(e => e is SkipActivityLogAttribute))
                return;

            var userIdClaim = context.HttpContext.User.Claims
                .FirstOrDefault(c =>
                    c.Type == ClaimTypes.NameIdentifier ||
                    c.Type == "sub");

            if (userIdClaim == null)
                return;

            var userId = int.Parse(userIdClaim.Value);

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            await _logger.LogAsync(
                userId,
                "Action",
                controller,
                action
            );
        }
    }
}
