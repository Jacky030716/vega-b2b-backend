namespace CleanArc.Infrastructure.Persistence.Settings;

public class MailSettings
{
    public const string SectionName = nameof(MailSettings);

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string DisplayName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
