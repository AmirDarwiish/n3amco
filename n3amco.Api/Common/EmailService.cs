// Services/EmailService.cs
using DairySystem.Api.Common;
using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendLeadNotificationAsync(string leadName, string phone, string email, string source)
    {
        var smtpClient = new SmtpClient(_config["Email:SmtpHost"])
        {
            Port = int.Parse(_config["Email:SmtpPort"]!),
            Credentials = new NetworkCredential(
                _config["Email:Username"],
                _config["Email:Password"]
            ),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(_config["Email:Username"]!),
            Subject = "🔔 ليد جديد وصلك!",
            Priority = MailPriority.High,  // 👈 أضف دي
            Body = $@"
                <div dir='rtl' style='font-family:Arial;'>
                    <h2 style='color:#2563eb;'>ليد جديد 🎯</h2>
                    <p><b>الاسم:</b> {leadName}</p>
                    <p><b>التليفون:</b> {phone}</p>
                    <p><b>الإيميل:</b> {email ?? "—"}</p>
                    <p><b>المصدر:</b> {source ?? "—"}</p>
                    <hr/>
                    <small>روح اتشيك الليدز دلوقتي 👇</small>
                </div>
            ",
            IsBodyHtml = true
        };
        message.To.Add(_config["Email:NotifyTo"]!);

        await smtpClient.SendMailAsync(message);
    }
}