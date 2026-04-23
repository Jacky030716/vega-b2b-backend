namespace CleanArc.Infrastructure.Persistence.Settings;

public class OllamaChallengeOptions
{
  public const string SectionName = nameof(OllamaChallengeOptions);

  public string BaseUrl { get; set; } = "http://localhost:11434";
  public string EmbeddingModel { get; set; } = "nomic-embed-text";
  public string EmbedEndpoint { get; set; } = "/api/embed";
  public string EmbedFallbackEndpoint { get; set; } = "/api/embeddings";
  public int RequestTimeoutSeconds { get; set; } = 120;
}
