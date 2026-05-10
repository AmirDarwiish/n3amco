namespace n3amco.Api.Users.Services
{
    public class UserActivityLogger : IUserActivityLogger
    {
        private readonly ApplicationDbContext _context;

        public UserActivityLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            int userId,
            string activityType,
            string? entityName = null,
            string? actionName = null,
            int? entityId = null)
        {
            _context.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                EntityName = entityName,
                ActionName = actionName,
                EntityId = entityId
            });

            await _context.SaveChangesAsync();
        }
    }

}
