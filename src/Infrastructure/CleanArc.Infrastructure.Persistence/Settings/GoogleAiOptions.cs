namespace CleanArc.Infrastructure.Persistence.Settings;

public class GoogleAiOptions
{
  public const string SectionName = nameof(GoogleAiOptions);

  public string ApiKey { get; set; } = string.Empty;
  public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
  public string ModelId { get; set; } = "gemini-3-flash-preview";
  public int TimeoutSeconds { get; set; } = 60;
}
