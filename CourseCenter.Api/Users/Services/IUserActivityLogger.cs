namespace CourseCenter.Api.Users.Services
{
    public interface IUserActivityLogger
    {
        Task LogAsync(
            int userId,
            string activityType,
            string? entityName = null,
            string? actionName = null,
            int? entityId = null
        );
    }

}
