namespace DairySystem.Api.Common
{
    public interface IEmailService
    {
        Task SendLeadNotificationAsync(string leadName, string phone, string email, string source);
    }
}
