namespace CleanArc.Infrastructure.Persistence.Settings;

public class GoogleImageAiOptions
{
  public const string SectionName = nameof(GoogleImageAiOptions);

  public string ApiKey { get; set; } = string.Empty;
  public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
  public string ModelId { get; set; } = "gemini-2.5-flash-image";
  public int TimeoutSeconds { get; set; } = 120;
}

