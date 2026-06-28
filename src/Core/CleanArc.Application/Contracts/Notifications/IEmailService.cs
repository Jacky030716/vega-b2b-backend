namespace CleanArc.Application.Contracts.Notifications;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}
