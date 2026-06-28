using System.Net;
using System.Net.Mail;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Notifications;

public class EmailService : IEmailService
{
    private readonly MailSettings _mailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger)
    {
        _mailSettings = mailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(_mailSettings.Host) || string.IsNullOrWhiteSpace(_mailSettings.FromEmail))
        {
            _logger.LogWarning("SMTP is not configured (Host/FromEmail empty). Falling back to logging.");
            LogEmail(toEmail, subject, body);
            return;
        }

        try
        {
            var displayName = string.IsNullOrWhiteSpace(_mailSettings.DisplayName) ? _mailSettings.FromEmail : _mailSettings.DisplayName;
            using var message = new MailMessage
            {
                From = new MailAddress(_mailSettings.FromEmail, displayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_mailSettings.Host, _mailSettings.Port)
            {
                EnableSsl = _mailSettings.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_mailSettings.UserName) && !string.IsNullOrWhiteSpace(_mailSettings.Password))
            {
                client.Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP. Falling back to logging.", toEmail);
            LogEmail(toEmail, subject, body);
        }
    }

    private void LogEmail(string toEmail, string subject, string body)
    {
        _logger.LogInformation(
            "----- MOCKED EMAIL START -----\n" +
            "To: {ToEmail}\n" +
            "Subject: {Subject}\n" +
            "Body:\n{Body}\n" +
            "----- MOCKED EMAIL END -----", 
            toEmail, subject, body);
    }
}
